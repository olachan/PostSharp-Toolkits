using System;
using System.Diagnostics;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;

namespace Threading
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    [IntroduceInterface(typeof(ISynchronized), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [ProvideAspectRole(StandardRoles.Threading)]
    [Conditional("DEBUG")]
    public class SingleThreadedInstanceAttribute : MethodInterceptionAspect, IInstanceScopedAspect, ISynchronized
    {
        [NonSerialized]
        private object @lock;

        [NonSerialized]
        private object instanceLock;

        private bool instanceLocked;

        public object Lock { get { return this.@lock; } } 

        public SingleThreadedInstanceAttribute(bool isInstanceLocked)
        {
            this.instanceLocked = isInstanceLocked;
        }

        public object CreateInstance(AdviceArgs aspectArgs)
        {
            var instance = new SingleThreadedInstanceAttribute(this.instanceLocked);
            return instance;
        }

        public void RuntimeInitializeInstance()
        {
            if (!this.instanceLocked)
            {
                this.instanceLock = new object();
            }
            else
            {
                this.@lock = new object();
            }
        }


        public override void OnInvoke(MethodInterceptionArgs args)
        {
            object l = this.instanceLocked ? ((ISynchronized)args.Instance).Lock : this.instanceLock;

            if (!Monitor.TryEnter(l))
            {
                throw new SingleThreadedException();
            }
            try
            {
                args.Proceed();
            }
            finally
            {
                Monitor.Exit(l);
            }
        }
    }
}
