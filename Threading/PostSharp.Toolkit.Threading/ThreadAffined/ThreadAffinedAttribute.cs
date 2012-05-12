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

namespace PostSharp.Toolkit.Threading.ThreadAffined
{
    [AttributeUsage(AttributeTargets.Class)]
    [IntroduceInterface(typeof(IThreadAffined), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [ProvideAspectRole(StandardRoles.Threading)]
    [Serializable]
    [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
    public class ThreadAffinedAttribute : OnMethodBoundaryAspect, IThreadAffined, IInstanceScopedAspect
    {
        [NonSerialized]
        private SynchronizationContext _synchronizationContext;
        
        SynchronizationContext IThreadAffined.SynchronizationContext
        {
            get { return _synchronizationContext; }
            set { _synchronizationContext = value; }
        }
        
        public override bool CompileTimeValidate(MethodBase method)
        {
            return method.IsConstructor;
        }

        public override void OnSuccess(MethodExecutionArgs args)
        {
            base.OnSuccess(args);
            var synchronizationContext = SynchronizationContext.Current;

            //Sometimes there's still no SynchronizationContext, even though Dispatcher is already available
            if (synchronizationContext == null)
            {
                //Cannot use Dispacther.CurrentDispatcher, because it might create a new Dispatcher
                var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                if (dispatcher != null)
                {
                    synchronizationContext = new DispatcherSynchronizationContext(dispatcher);
                }
            }

            if (synchronizationContext == null)
            {
                throw new InvalidOperationException(
                    "Instances of classes marked with ThreadAffinedAttribute can only be crated on threads with synchronization contexts " +
                    "(typically WPF or Windows.Forms UI threads).");
            }
            ((IThreadAffined) args.Instance).SynchronizationContext = synchronizationContext;
        }

        public object CreateInstance(AdviceArgs adviceArgs)
        {
            return new ThreadAffinedAttribute();
        }

        public void RuntimeInitializeInstance()
        {
            
        }
    }
}