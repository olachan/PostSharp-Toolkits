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
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Custom attribute that, when applied on a method, specifies that it should be executed in UI thread. 
    /// When applied on a method it automatically applies <see cref="DispatcherObjectAspect"/> on object implementing the method. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Method )]
    [ProvideAspectRole( StandardRoles.Threading )]
    [Serializable]
    [MulticastAttributeUsage( MulticastTargets.Method, TargetExternalMemberAttributes = MulticastAttributes.Internal )]
    public sealed class DispatchedMethodAttribute : MethodInterceptionAspect, IAspectProvider
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


            if ( this.IsAsync &&
                (methodInfo.ReturnType != typeof(void) || methodInfo.GetParameters().Any(p => p.ParameterType.IsByRef)))
            {
                ThreadingMessageSource.Instance.Write(method, SeverityType.Error, "THR001", method.DeclaringType.Name, method.Name);
                
                return false;
            }

            return base.CompileTimeValidate( method );
        }

        public override void OnInvoke( MethodInterceptionArgs args )
        {
            IDispatcherObject threadAffined = args.Instance as IDispatcherObject;
            IDispatcher dispatcher = threadAffined.Dispatcher;

            if ( dispatcher == null )
            {
                throw new InvalidOperationException( "Cannot dispatch method: synchronization context is null" );
            }


            if ( this.IsAsync )
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


        public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
        {
            Type declaringType = ((MethodBase) targetElement).DeclaringType;
            if ( !typeof(IDispatcherObject).IsAssignableFrom( declaringType ) )
            {
                return typeLevelAspects.GetAspect( declaringType,
                                                   type => new AspectInstance( type, new ObjectConstruction( typeof(DispatcherObjectAspect) ), null ) );
            }
            else
            {
                return new AspectInstance[0];
            }
        }

        /// <summary>
        /// Custom attribute that, when applied on a class tries to find <see cref="SynchronizationContext"/> or for current object.
        /// </summary>
        [IntroduceInterface( typeof(IDispatcherObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore
            )]
        [ProvideAspectRole( StandardRoles.Threading )]
        [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
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
                    IDispatcher dispatcher = WpfDispatcherBinding.TryFindWpfDispatcher(Thread.CurrentThread);

                    if (dispatcher != null)
                    {
                        this.dispatcher = dispatcher;
                    }
                    
                }

                if ( this.dispatcher == null )
                {
                    throw new InvalidOperationException(
                        "Instances of classes marked with DispatcherObjectAspect can only be crated on threads with synchronization contexts " +
                        "(typically WPF or Windows.Forms UI threads)." );
                }
            }

            
        }
    }
}