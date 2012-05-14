using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading.SingleThreaded
{
    /// <summary>
    /// Custom attribute that, when applied on a method, ensures that only one thread executes in the method
    /// using a simple <see cref="Monitor"/>. When more than one thread accesses the method <see cref="SingleThreadedException"/> is thrown. 
    /// This attribute works only in DEBUG compilation. 
    /// </summary>
    /// <remarks>
    /// When isInstanceLocked is set to true static methods lock on the type, whereas instance methods lock on the instance. 
    /// Only one thread can execute in any of the instance functions, and only one thread can execute in any of a class's static functions.
    /// When isInstanceLocked is set to false only one thread can execute in specific method but many threads can execute in diferent methods.
    /// </remarks>
    [Conditional("DEBUG")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class SingleThreadedAttribute : MethodLevelAspect, IAspectProvider
    {
        private bool instanceLocked;

        public SingleThreadedAttribute(bool isInstanceLocked = true)
        {
            this.instanceLocked = isInstanceLocked;
        }

        public IEnumerable<AspectInstance> ProvideAspects(object targetElement)
        {
            MethodBase method = (MethodBase)targetElement;

            if (!method.IsStatic)
            {
                yield return new AspectInstance(targetElement, new SingleThreadedInstanceAttribute(this.instanceLocked));
            }
            else
            {
                yield return new AspectInstance(targetElement, new SingleThreadedStaticAttribute(this.instanceLocked));
            }
        }
    }
}
