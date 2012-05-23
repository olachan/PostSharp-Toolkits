using System;
using System.Diagnostics;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Toolkit.Threading.Synchronization;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    [ProvideAspectRole(StandardRoles.Threading)]
    [Conditional("DEBUG")]
    public class SingleThreadedStaticAttribute : LockStaticAttributeBase
    {
        //TODO: Replace with some .NET 3.5 compatible collection

        public SingleThreadedStaticAttribute(bool isInstanceLocked = true)
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
