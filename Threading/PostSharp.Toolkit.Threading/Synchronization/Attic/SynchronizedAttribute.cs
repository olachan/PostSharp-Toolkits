using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    /// <summary>
    /// Custom attribute that, when applied on a method, synchronizes its execution
    /// using a simple <see cref="Monitor"/>.
    /// </summary>
    /// <remarks>
    /// When isInstanceLocked is set to true static methods lock on the type, whereas instance methods lock on the instance. 
    /// Only one thread can execute in any of the instance functions, and only one thread can execute in any of a class's static functions.
    /// When isInstanceLocked is set to false only one thread can execute in specific method but many threads can execute in diferent methods.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class SynchronizedAttribute : MethodLevelAspect, IAspectProvider
    {
        private bool instanceLocked;

        /// <summary>
        /// Constructs new instance of SynchronizedAttribute.
        /// </summary>
        /// <param name="isInstanceLocked">Specyfies isInstanceLocked property of constructed attribute</param>
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
