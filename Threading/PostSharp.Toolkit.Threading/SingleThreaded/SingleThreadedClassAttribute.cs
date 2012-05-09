using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading.SingleThreaded
{
    /// <summary>
    /// TODO: Description
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    [Conditional("DEBUG")]
    public class SingleThreadedClassAttribute : MethodLevelAspect, IAspectProvider
    {
        public bool IgnoreGetters { get; set; }
        public bool IgnoreSetters { get; set; }

        public SingleThreadedClassAttribute()
        {
            this.IgnoreGetters = true;
            this.IgnoreSetters = false;
        }

        public IEnumerable<AspectInstance> ProvideAspects(object targetElement)
        {
            MethodBase method = (MethodBase)targetElement;

            if (method.IsConstructor) yield break;

            if (method.IsSpecialName)
            {
                if (this.IgnoreGetters && method.Name.StartsWith("get_")) yield break;
                if (this.IgnoreSetters && method.Name.StartsWith("set_")) yield break;
            }

            yield return new AspectInstance(targetElement, new SingleThreadedAttribute());
        }
    }
}