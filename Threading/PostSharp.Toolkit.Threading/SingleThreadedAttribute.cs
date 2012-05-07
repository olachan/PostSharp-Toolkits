using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using PostSharp.Aspects;

namespace Threading
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Conditional("DEBUG")]
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
