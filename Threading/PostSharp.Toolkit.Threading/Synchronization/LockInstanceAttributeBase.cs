// -----------------------------------------------------------------------
// <copyright file="LockInstanceAttributeBase.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using PostSharp.Aspects;
using PostSharp.Toolkit.Threading.DeadlockDetection;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    [Serializable]
    public class LockInstanceAttributeBase : MethodInterceptionAspect
    {
        [NonSerialized]
        protected object instanceLock;

        [NonSerialized]
        protected object attributeLock;

        protected bool useDeadlockDetection;

        protected bool instanceLocked;

        public LockInstanceAttributeBase(bool isInstanceLocked = true)
        {
            this.instanceLocked = isInstanceLocked;
        }

        public override void CompileTimeInitialize(System.Reflection.MethodBase method, AspectInfo aspectInfo)
        {
            base.CompileTimeInitialize(method, aspectInfo);

            var attributes = Attribute.GetCustomAttributes(method.DeclaringType.Assembly, typeof(DeadlockDetectionPolicy));
            this.useDeadlockDetection = attributes.Length > 0;
        }

        public void RuntimeInitializeInstance()
        {
            if (!this.instanceLocked)
            {
                this.attributeLock = new object();
            }
            else
            {
                this.instanceLock = new object();
            }
        }
    }
}
