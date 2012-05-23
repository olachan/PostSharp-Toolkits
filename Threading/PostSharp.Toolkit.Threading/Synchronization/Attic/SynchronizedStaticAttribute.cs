using System;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Toolkit.Threading.Deadlock;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    /// <summary>
    /// For internal use. Use <see cref="SynchronizedAttribute"/> insted.
    /// </summary>
    /// <remarks>
    /// Implements synchronizatian for static methods.
    /// </remarks>
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

            if (useDeadlockDetection)
            {
                MonitorWrapper.Enter(l);
            }
            else
            {
                Monitor.Enter(l);
            }

            try
            {
                args.Proceed();
            }
            finally
            {
                if (useDeadlockDetection)
                {
                    MonitorWrapper.Exit(l);
                }
                else
                {
                    Monitor.Exit(l);
                }
            }
        }
    }
}
