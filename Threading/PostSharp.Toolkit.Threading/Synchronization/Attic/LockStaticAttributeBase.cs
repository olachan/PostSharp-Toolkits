using System;
using System.Collections.Concurrent;
using PostSharp.Aspects;
using PostSharp.Toolkit.Threading.Deadlock;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    [Serializable]
    public class LockStaticAttributeBase : MethodInterceptionAspect
    {
        protected static ConcurrentDictionary<Type, object> typeLocks = new ConcurrentDictionary<Type, object>();

        [NonSerialized]
        protected object attributeLock;

        protected bool instanceLocked;

        protected bool useDeadlockDetection;


        public LockStaticAttributeBase(bool isInstanceLocked = true)
        {
            this.instanceLocked = isInstanceLocked;
        }

        public override void CompileTimeInitialize(System.Reflection.MethodBase method, AspectInfo aspectInfo)
        {
            base.CompileTimeInitialize(method, aspectInfo);

            var attributes = Attribute.GetCustomAttributes(method.DeclaringType.Assembly, typeof(DeadlockDetectionPolicy));
            this.useDeadlockDetection = attributes.Length > 0;
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
