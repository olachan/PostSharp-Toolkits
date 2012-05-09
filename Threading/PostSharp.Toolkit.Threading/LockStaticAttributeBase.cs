using System;
using System.Collections.Concurrent;

using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading
{
    [Serializable]
    public class LockStaticAttributeBase : MethodInterceptionAspect
    {
        // TODO Replace with something .NET 3.5 compatibile
        protected static ConcurrentDictionary<Type, object> typeLocks = new ConcurrentDictionary<Type, object>();

        [NonSerialized]
        protected object attributeLock;

        protected bool instanceLocked;

        public LockStaticAttributeBase(bool isInstanceLocked = true)
        {
            this.instanceLocked = isInstanceLocked;
        }

        public override void RuntimeInitialize(System.Reflection.MethodBase method)
        {
            if (!instanceLocked)
            {
                this.attributeLock = new object();
            }
        }
    }
}
