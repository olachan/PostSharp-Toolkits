using System;
using System.Collections.Concurrent;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;

namespace Threading
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    [CompositionAspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
    [ProvideAspectRole(StandardRoles.Threading)]
    public class SingleThreadedStaticAttribute : MethodInterceptionAspect
    {
        //TODO: Replace with some .NET 3.5 compatible collection
        private static ConcurrentDictionary<Type, object> staticLocks = new ConcurrentDictionary<Type, object>();

        [NonSerialized]
        private object instanceLock;

        private bool instanceLocked;

        public SingleThreadedStaticAttribute(bool isInstanceLocked)
        {
            this.instanceLocked = isInstanceLocked;
        }

        public override void RuntimeInitialize(System.Reflection.MethodBase method)
        {
            if (!instanceLocked)
            {
                this.instanceLock = new object();
            }
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            object l;

            if (this.instanceLocked)
            {
                l = staticLocks.GetOrAdd(args.Method.DeclaringType, (key) => new object());
            }
            else
            {
                l = this.instanceLock;
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
