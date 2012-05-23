using System;
using System.Diagnostics;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Toolkit.Threading.Synchronization;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    [AspectTypeDependency(AspectDependencyAction.Commute, typeof(SingleThreadedInstanceAttribute))]
    [AspectTypeDependency(AspectDependencyAction.Commute, typeof(SynchronizedInstanceAttribute))]
    [IntroduceInterface(typeof(ISingleThreaded), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [ProvideAspectRole(StandardRoles.Threading)]
    [Conditional("DEBUG")]
    public class SingleThreadedInstanceAttribute : LockInstanceAttributeBase, IInstanceScopedAspect, ISingleThreaded
    {
        public SingleThreadedInstanceAttribute(bool isInstanceLocked = true)
            : base(isInstanceLocked)
        {
        }

        public object CreateInstance(AdviceArgs aspectArgs)
        {
            var instance = new SingleThreadedInstanceAttribute(this.instanceLocked);
            return instance;
        }

        public object Lock
        {
            get
            {
                return this.instanceLock;
            }
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            object l = this.instanceLocked ? ((ISingleThreaded)args.Instance).Lock : this.attributeLock;

            if (!Monitor.TryEnter(l))
            {
                throw new ThreadUnsafeException();
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
