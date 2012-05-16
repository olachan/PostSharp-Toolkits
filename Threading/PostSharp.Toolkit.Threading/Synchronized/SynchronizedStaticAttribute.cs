using System;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Toolkit.Threading.Deadlock;

namespace PostSharp.Toolkit.Threading.Synchronized
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

            Monitor.Enter(l);

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
