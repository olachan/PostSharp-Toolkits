// -----------------------------------------------------------------------
// <copyright file="LockInstanceAttributeBase.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;

using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading
{
    [Serializable]
    public class LockInstanceAttributeBase : MethodInterceptionAspect
    {
        [NonSerialized]
        protected object instanceLock;

        [NonSerialized]
        protected object attributeLock;

        protected bool instanceLocked;

        public LockInstanceAttributeBase(bool isInstanceLocked = true)
        {
            this.instanceLocked = isInstanceLocked;
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
