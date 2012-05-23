#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    [AttributeUsage( AttributeTargets.Method )]
    [ProvideAspectRole( StandardRoles.Threading )]
    [Serializable]
    [MulticastAttributeUsage( MulticastTargets.Method, TargetExternalMemberAttributes = MulticastAttributes.Internal )]
    public class DispatchedMethodAttribute : MethodInterceptionAspect, IAspectProvider
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


            if ( this.IsAsync && methodInfo.ReturnType != typeof(void) )
            {
                Message.Write( method, SeverityType.Error, "THREADING.DISPATCH02",
                               "Asynchronous DispatchedMethodAttribute cannot be applied to {0}.{1}. It can only be applied to void methods.",
                               method.DeclaringType.Name, method.Name );
                return false;
            }

//            if (!typeof(IDispatcherObject).IsAssignableFrom( method.DeclaringType ) &&
//                    method.DeclaringType.GetCustomAttributes(typeof(DispatcherObjectAspect), true).Length == 0)
//            {
//
//                Message.Write(method, SeverityType.Error, "THREADING.DISPATCH03",
//                               "DispatchedMethodAttribute cannot be applied to {0}.{1}. It can only be applied to methods in classes implementing "+
//                               "IDispatcherObject or marked with DispatcherObjectAspect.",
//                               method.DeclaringType.Name, method.Name);
//            }

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
                    //Sometimes there's still no Dispatcher, even though Dispatcher is already available

                    // Cannot use Dispacther.CurrentDispatcher, because it might create a new Dispatcher
                    Dispatcher dispatcher = Dispatcher.FromThread( Thread.CurrentThread );

                    if ( dispatcher != null )
                    {
                        this.dispatcher = new DispatcherWrapper( dispatcher );
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