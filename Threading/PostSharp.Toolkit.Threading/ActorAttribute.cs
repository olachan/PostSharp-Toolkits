#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Threading;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Aspects supporting implementation of actor-base messaging pattern.
    /// See <see cref="Actor"/> for details.
    /// </summary>
    [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
    // [AspectRoleDependency(AspectDependencyAction.Conflict, ThreadingToolkitAspectRoles.ThreadingModel)]
    [ProvideAspectRole(ThreadingToolkitAspectRoles.ThreadingModel)]
    public sealed class ActorAttribute : TypeLevelAspect, IAspectProvider
    {

        public override bool CompileTimeValidate(Type type)
        {
            bool result = base.CompileTimeValidate(type);

            // Check that the attribute is applied on a class derived from Actor (should not be used manually anyway). [ERROR]
            if (!typeof(Actor).IsAssignableFrom(type))
            {
                ThreadingMessageSource.Instance.Write(type, SeverityType.Error, "THR004", type.Name);
                result = false;
            }

            // Check that all fields are private or protected. [ERROR]
            foreach (var publicField in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) // .Where( f => f.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length == 0 )
            {
                ThreadingMessageSource.Instance.Write(type, SeverityType.Error, "THR005", type.Name, publicField.Name);
                result = false;
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes(typeof(ThreadSafeAttribute), true).Length == 0 && ReflectionHelper.IsInternalOrPublic(m, false)))
            {
                // Check that the method returns void and does not have ref/out parameters.

                if ((method.ReturnType != typeof(void) && GetStateMachineType(method) == null) || method.GetParameters().Any(p => p.ParameterType.IsByRef))
                {
                    ThreadingMessageSource.Instance.Write(method, SeverityType.Error, "THR009", method.DeclaringType.Name, method.Name);

                }
            }

            return result;
        }


        // TODO: Check that instance fields, and unprotected instance methods, are only accessed by instance methods, and from the 'this' object. [WARNING]

        // TODO: Cope with callbacks (delegate): make dispatchable if we can, otherwise check that thread is ok (IDispatcher.CheckAccess())

        // TODO: Private interface implementations should be dispatched too.

        public IEnumerable<MethodBase> SelectVoidMethods(Type type)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (method.ReturnType == typeof(void) &&
                    GetStateMachineType(method) == null &&
                     method.GetCustomAttributes(typeof(ThreadSafeAttribute), false).Length == 0 &&
                     ReflectionHelper.IsInternalOrPublic(method, false))
                {
                    yield return method;
                }

                /*
                ReflectionSearch.GetMethodsUsingDeclaration( method ).Any(
                    reference => (reference.Instructions & MethodUsageInstructions.LoadMethodAddress | MethodUsageInstructions.LoadMethodAddressVirtual) != 0 );
                 */
            }
        }

        [OnMethodInvokeAdvice, MethodPointcut("SelectVoidMethods")]
        public void OnVoidMethodInvoke(MethodInterceptionArgs args)
        {
            if (((Actor)args.Instance).IsDisposed) throw new ObjectDisposedException(args.Instance.ToString());
            ((IDispatcherObject)args.Instance).Dispatcher.BeginInvoke(new ActorWorkItem(args, true));
        }

        private static Type GetStateMachineType(MethodInfo method)
        {
            CustomAttributeInstance customAttribute = ReflectionSearch.GetCustomAttributesOnTarget(method).SingleOrDefault(
                attribute => attribute.Construction.TypeName.StartsWith("System.Runtime.CompilerServices.AsyncStateMachineAttribute"));

            return customAttribute == null ? null : (Type)customAttribute.Construction.ConstructorArguments[0];
        }

        IEnumerable<AspectInstance> IAspectProvider.ProvideAspects(object targetElement)
        {
            Type type = (Type)targetElement;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                Type stateMachineType = GetStateMachineType(method);

                if (stateMachineType != null)
                {
                    yield return new AspectInstance(stateMachineType, new StateMachineEnhancements());
                }
            }
        }

        [Serializable]
        public class StateMachineEnhancements : TypeLevelAspect
        {

            static Action<Action> callYieldDelegate;
            static Action<object> callMoveNextDelegate;

            Func<object, Actor> getActorDelegate;
            LocationInfo thisField;

            public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
            {
                this.thisField = LocationInfo.ToLocationInfo(type.GetFields(BindingFlags.Instance | BindingFlags.Public).SingleOrDefault(f => f.Name.EndsWith("__this")));
                if (this.thisField == null)
                {
                    Message.Write(MessageLocation.Of(type), SeverityType.Error, "XXX", "Cannot find the 'this' field in the state machine.");
                }
            }

            public override void RuntimeInitialize(Type type)
            {

                ParameterExpression instanceParameter = Expression.Parameter(typeof(object));

                // Generate code to get the 'this' field from the state machine.

                getActorDelegate = Expression.Lambda<Func<object, Actor>>(Expression.Field(
                    Expression.Convert(instanceParameter, type), this.thisField.FieldInfo), instanceParameter).Compile();


                if (callYieldDelegate == null)
                {
                    // Here, we are using LINQ expressions to avoid linking this assembly to .NET 4.5 only because of the async/await feature.
                    ParameterExpression actionParameter = Expression.Parameter(typeof(Action));
                    Expression callYield = Expression.Call(typeof(Task).GetMethod("Yield", Type.EmptyTypes));
                    Expression callGetAwaiter = Expression.Call(callYield,
                        Type.GetType("System.Runtime.CompilerServices.YieldAwaitable").GetMethod("GetAwaiter", Type.EmptyTypes));
                    Expression callUnsafeOnCompleted = Expression.Call(callGetAwaiter, Type.GetType("System.Runtime.CompilerServices.YieldAwaitable+YieldAwaiter").GetMethod("UnsafeOnCompleted", new Type[] { typeof(Action) }), actionParameter);
                    callYieldDelegate = Expression.Lambda<Action<Action>>(callUnsafeOnCompleted, actionParameter).Compile();

                    callMoveNextDelegate =
                        Expression.Lambda<Action<object>>(
                        Expression.Call(
                        Expression.Convert(instanceParameter, Type.GetType("System.Runtime.CompilerServices.IAsyncStateMachine")),
                        Type.GetType("System.Runtime.CompilerServices.IAsyncStateMachine").GetMethod("MoveNext", Type.EmptyTypes)), instanceParameter).Compile();
                }
            }

            IEnumerable<MethodInfo> SelectMoveNext(Type type)
            {
                return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name.EndsWith("MoveNext"));
            }

            [OnMethodEntryAdvice, MethodPointcut("SelectMoveNext")]
            public void BeforeMoveNext(MethodExecutionArgs args)
            {
                Actor actor = getActorDelegate(args.Instance);
                if (!actor.Dispatcher.CheckAccess())
                {
                    SynchronizationContext old = SynchronizationContext.Current;
                    try
                    {
                        SynchronizationContext.SetSynchronizationContext(actor.Dispatcher.SynchronizationContext);
                        callYieldDelegate(() => callMoveNextDelegate(args.Instance));
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(old);
                    }

                    args.FlowBehavior = FlowBehavior.Return;
                }

            }

        }






    }
}