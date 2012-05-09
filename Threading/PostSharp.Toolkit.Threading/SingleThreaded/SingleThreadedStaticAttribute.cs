using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;

namespace PostSharp.Toolkit.Threading.SingleThreaded
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
