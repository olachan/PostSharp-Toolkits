using System;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Toolkit.Threading.Deadlock;

namespace PostSharp.Toolkit.Threading.Synchronized
{
    /// <summary>
    /// For internal use. Use <see cref="SynchronizedAttribute"/> insted.
    /// </summary>
    /// <remarks>
    /// Implements synchronizatian for instance methods.
    /// </remarks>
    [Serializable]
    [IntroduceInterface(typeof(ISynchronized), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [ProvideAspectRole(StandardRoles.Threading)]
    public class SynchronizedInstanceAttribute : LockInstanceAttributeBase, IInstanceScopedAspect, ISynchronized
    {
        /// <summary>
        /// Initializes a new <see cref="SynchronizedInstanceAttribute"/>.
        /// </summary>
        public SynchronizedInstanceAttribute(bool isInstanceLocked = true)
            : base(isInstanceLocked)
        {
            this.AspectPriority = 2;
        }

        public object CreateInstance(AdviceArgs aspectArgs)
        {
            var instance = new SynchronizedInstanceAttribute(this.instanceLocked);
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
            object l = this.instanceLocked ? ((ISynchronized)args.Instance).Lock : this.attributeLock;

            // TODO: deadLock detection logic review
            DeadlockMonitor.EnterWaiting(l, ResourceType.Lock, null);

            if (!Monitor.TryEnter(l, 200))
            {
                DeadlockMonitor.DetectDeadlocks();
                Monitor.Enter(l);
            }
            DeadlockMonitor.ConvertWaitingToAcquired(l, ResourceType.Lock, null);

            try
            {
                args.Proceed();
            }
            finally
            {
                Monitor.Exit(l);
                DeadlockMonitor.ExitAcquired(l, ResourceType.Lock);
            }
        }
    }
}