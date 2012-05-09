using System;
using System.Collections.Generic;
using System.Reflection;

using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading.Synchronized
{
    /// <summary>
    /// TODO: Description
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public class SynchronizedClassAttribute : MethodLevelAspect, IAspectProvider
    {
        public bool IgnoreGetters { get; set; }

        public SynchronizedClassAttribute()
        {
            this.IgnoreGetters = true;
        }

        public IEnumerable<AspectInstance> ProvideAspects(object targetElement)
        {
            MethodBase method = (MethodBase)targetElement;

            if (method.IsConstructor) yield break;

            if (method.IsSpecialName)
            {
                if (this.IgnoreGetters && method.Name.StartsWith("get_")) yield break;
            }

            yield return new AspectInstance(targetElement, new SynchronizedAttribute());
        }
    }
}