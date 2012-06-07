#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Constraints;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Custom attribute that, when applied on a method, specifies that it should be executed in UI thread.
    /// Supports WinForms, WPF, and any class implementing the <see cref="IDispatcherObject"/> manually.
    /// </summary>
    [AttributeUsage( AttributeTargets.Method )]
    [ProvideAspectRole( StandardRoles.Threading )]
    [Serializable]
    [MulticastAttributeUsage( MulticastTargets.Method, TargetExternalMemberAttributes = MulticastAttributes.Internal, AllowMultiple = false )]
    public sealed class DispatchedMethodAttribute : MethodLevelAspect, IAspectProvider
    {
        private static readonly TypeLevelAspectRepository typeLevelAspects;

        static DispatchedMethodAttribute()
        {
            if ( PostSharpEnvironment.IsPostSharpRunning )
            {
                typeLevelAspects = new TypeLevelAspectRepository();
            }
        }

        public bool IsAsync { get; set; }

        public DispatchedMethodAttribute( bool isAsync = false )
        {
            this.IsAsync = isAsync;
        }

        public override bool CompileTimeValidate( MethodBase method )
        {
            MethodInfo methodInfo = (MethodInfo) method;


            if ( this.IsAsync )
            {
                Type stateMachineType = GetStateMachineType( methodInfo );


                if ( ((methodInfo.ReturnType != typeof(void) && stateMachineType == null) || methodInfo.GetParameters().Any( p => p.ParameterType.IsByRef )) )
                {
                    ThreadingMessageSource.Instance.Write( method, SeverityType.Error, "THR001", method.DeclaringType.Name, method.Name );

                    return false;
                }
            }

            return true;
        }

        internal static Type GetStateMachineType( MethodInfo method )
        {
            CustomAttributeInstance customAttribute = ReflectionSearch.GetCustomAttributesOnTarget( method ).SingleOrDefault(
                attribute => attribute.Construction.TypeName.StartsWith( "System.Runtime.CompilerServices.AsyncStateMachineAttribute" ) );

            return customAttribute == null ? null : (Type) customAttribute.Construction.ConstructorArguments[0];
        }


        public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
        {
            // Implement IDispatcherObject.
            MethodInfo methodInfo = (MethodInfo) targetElement;
            Type declaringType = methodInfo.DeclaringType;

            if ( !typeof(IDispatcherObject).IsAssignableFrom( declaringType ) )
            {
                AspectInstance aspectInstance = typeLevelAspects.GetAspect( declaringType,
                                                                            type =>
                                                                            new AspectInstance( type, new ObjectConstruction( typeof(DispatcherObjectAspect) ),
                                                                                                null ) );
                if ( aspectInstance != null )
                    yield return aspectInstance;
            }

            // Add aspect to the async state machine.
            Type stateMachineType = GetStateMachineType( methodInfo );
            if ( stateMachineType != null )
            {
                yield return new AspectInstance( stateMachineType, new AsyncStateMachineAspect() );
            }
            else
            {
                yield return new AspectInstance( methodInfo, new DispatchedMethodAspect( this.IsAsync ) );
            }
        }

        [Serializable]
        [Internal]
        public sealed class DispatchedMethodAspect : MethodInterceptionAspect
        {
            private readonly bool isAsync;

            internal DispatchedMethodAspect( bool isAsync )
            {
                this.isAsync = isAsync;
            }

            public override void OnInvoke( MethodInterceptionArgs args )
            {
                IDispatcherObject threadAffined = args.Instance as IDispatcherObject;
                IDispatcher dispatcher = threadAffined.Dispatcher;

                if ( dispatcher == null )
                {
                    throw new InvalidOperationException( "Cannot dispatch method: synchronization context is null" );
                }


                if ( this.isAsync )
                {
                    dispatcher.BeginInvoke( new WorkItem( args, true ) );
                }
                else if ( dispatcher.CheckAccess() )
                {
                    args.Proceed();
                }
                else
                {
                    dispatcher.Invoke( new WorkItem( args ) );
                }
            }
        }

        [Serializable]
        [Internal]
        public sealed class AsyncStateMachineAspect : TypeLevelAspect
        {
            private static Action<Action> callYieldDelegate;
            private static Action<object> callMoveNextDelegate;

            private Func<object, IDispatcherObject> getActorDelegate;
            private LocationInfo thisField;

            public override void CompileTimeInitialize( Type type, AspectInfo aspectInfo )
            {
                this.thisField =
                    LocationInfo.ToLocationInfo( type.GetFields( BindingFlags.Instance | BindingFlags.Public ).SingleOrDefault( f => f.Name.EndsWith( "__this" ) ) );
                if ( this.thisField == null )
                {
                    ThreadingMessageSource.Instance.Write( type.DeclaringType, SeverityType.Error, "THR001", type );
                }
            }

            public override void RuntimeInitialize( Type type )
            {
                ParameterExpression instanceParameter = Expression.Parameter( typeof(object) );

                // Generate code to get the 'this' field from the state machine.

                this.getActorDelegate = Expression.Lambda<Func<object, IDispatcherObject>>( Expression.Field(
                    Expression.Convert( instanceParameter, type ), this.thisField.FieldInfo ), instanceParameter ).Compile();


                if ( callYieldDelegate == null )
                {
                    // Here, we are using LINQ expressions to avoid linking this assembly to .NET 4.5 only because of the async/await feature.
                    ParameterExpression actionParameter = Expression.Parameter( typeof(Action) );
                    Expression callYield = Expression.Call( typeof(Task).GetMethod( "Yield", Type.EmptyTypes ) );
                    Expression callGetAwaiter = Expression.Call( callYield,
                                                                 Type.GetType( "System.Runtime.CompilerServices.YieldAwaitable" ).GetMethod( "GetAwaiter",
                                                                                                                                             Type.EmptyTypes ) );
                    Expression callUnsafeOnCompleted = Expression.Call( callGetAwaiter,
                                                                        Type.GetType( "System.Runtime.CompilerServices.YieldAwaitable+YieldAwaiter" ).GetMethod(
                                                                            "UnsafeOnCompleted", new[] {typeof(Action)} ), actionParameter );
                    callYieldDelegate = Expression.Lambda<Action<Action>>( callUnsafeOnCompleted, actionParameter ).Compile();

                    callMoveNextDelegate =
                        Expression.Lambda<Action<object>>(
                            Expression.Call(
                                Expression.Convert( instanceParameter, Type.GetType( "System.Runtime.CompilerServices.IAsyncStateMachine" ) ),
                                Type.GetType( "System.Runtime.CompilerServices.IAsyncStateMachine" ).GetMethod( "MoveNext", Type.EmptyTypes ) ),
                            instanceParameter ).Compile();
                }
            }

            private IEnumerable<MethodInfo> SelectMoveNext( Type type )
            {
                return type.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Where( m => m.Name.EndsWith( "MoveNext" ) );
            }

            [OnMethodEntryAdvice, MethodPointcut( "SelectMoveNext" )]
            public void BeforeMoveNext( MethodExecutionArgs args )
            {
                // Before StateMachine.MoveNext is executed, we check that we are executing in the proper synchronization context.
                // If not, we do a  "await Task.Yield" from inside the proper synchronization context, which has the effect
                // of re-posting the call to the proper context. Typically, this happens only on the first call.

                IDispatcherObject actor = this.getActorDelegate( args.Instance );
                if ( !actor.Dispatcher.CheckAccess() )
                {
                    SynchronizationContext old = SynchronizationContext.Current;
                    try
                    {
                        SynchronizationContext.SetSynchronizationContext( actor.Dispatcher.SynchronizationContext );
                        callYieldDelegate( () => callMoveNextDelegate( args.Instance ) );
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext( old );
                    }

                    args.FlowBehavior = FlowBehavior.Return;
                }
            }
        }


        /// <summary>
        /// Custom attribute that, when applied on a class tries to find <see cref="SynchronizationContext"/> or for current object.
        /// </summary>
        [IntroduceInterface( typeof(IDispatcherObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore
            )]
        [ProvideAspectRole( StandardRoles.Threading )]
        [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
        [Internal]
        public sealed class DispatcherObjectAspect : IInstanceScopedAspect, IDispatcherObject
        {
            [NonSerialized] private IDispatcher dispatcher;

            IDispatcher IDispatcherObject.Dispatcher
            {
                get { return this.dispatcher; }
            }

            public object CreateInstance( AdviceArgs adviceArgs )
            {
                return new DispatcherObjectAspect();
            }

            public void RuntimeInitializeInstance()
            {
                SynchronizationContext synchronizationContext = SynchronizationContext.Current;

                if ( synchronizationContext != null )
                {
                    this.dispatcher = new SynchronizationContextWrapper( synchronizationContext );
                }
                else
                {
                    //Sometimes there's still no SynchronizationContext, even though Dispatcher is already available
                    IDispatcher dispatcher = WpfDispatcherBinding.TryFindWpfDispatcher( Thread.CurrentThread );

                    if ( dispatcher != null )
                    {
                        this.dispatcher = dispatcher;
                    }
                }

                if ( this.dispatcher == null )
                {
                    throw new InvalidOperationException(
                        "Instances of classes marked with DispatcherObjectAspect can only be crated on threads with synchronization contexts " +
                        "(typically WPF or Windows.Forms UI threads), or must implement IDispatcherObject manually." );
                }
            }
        }
    }
}