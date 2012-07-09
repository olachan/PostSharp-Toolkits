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
    [RequirePostSharp("PostSharp.Toolkit.Threading.Weaver", "PostSharp.Toolkit.Threading", AssemblyReferenceOnly = true)]
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
           // This class is intentionally left blank.
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