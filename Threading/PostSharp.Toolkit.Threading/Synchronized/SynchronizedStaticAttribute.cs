using System;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Toolkit.Threading.Deadlock;

namespace PostSharp.Toolkit.Threading.Synchronized
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    [ProvideAspectRole(StandardRoles.Threading)]
    public class SynchronizedStaticAttribute : LockStaticAttributeBase
    {
        public SynchronizedStaticAttribute(bool isInstanceLocked = true)
            : base(isInstanceLocked)
        {
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            object l;

            if (this.instanceLocked)
            {
                l = typeLocks.GetOrAdd(args.Method.DeclaringType, (key) => new object());
            }
            else
            {
                l = this.attributeLock;
            }

            // TODO: deadLock detection logic review
            DeadlockMonitor.EnterWaiting(l, null, null);

            if (!Monitor.TryEnter(l, 200))
            {
                DeadlockMonitor.DetectDeadlocks();
                Monitor.Enter(l);
            }
            DeadlockMonitor.ConvertWaitingToAcquired(l, null, null);
            
            try
            {
                args.Proceed();
            }
            finally
            {
                Monitor.Exit(l);
                DeadlockMonitor.ExitAcquired(l, null);
            }
        }
    }
}
