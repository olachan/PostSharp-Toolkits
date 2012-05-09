using System;
using System.Collections.Generic;
using System.Reflection;

using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading.Synchronized
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class SynchronizedAttribute : MethodLevelAspect, IAspectProvider
    {
        private bool instanceLocked;

        public SynchronizedAttribute(bool isInstanceLocked = true)
        {
            this.instanceLocked = isInstanceLocked;
        }

        public IEnumerable<AspectInstance> ProvideAspects(object targetElement)
        {
            MethodBase method = (MethodBase)targetElement;

            if (!method.IsStatic)
            {
                yield return new AspectInstance(targetElement, new SynchronizedInstanceAttribute(this.instanceLocked));
            }
            else
            {
                yield return new AspectInstance(targetElement, new SynchronizedStaticAttribute(this.instanceLocked));
            }
        }
    }
}
