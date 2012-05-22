using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    //TODO: Update summary!
    /// <summary>
    /// Custom attribute when applied on a class, automatically applies <see cref="SingleThreadedAttribute"/> to all methods of the class excluding property getters.
    /// Optionally IgnoreGetters can be set to false, then <see cref="SingleThreadedAttribute"/> will be applied to all methods including property getters.
    /// Optionally IgnoreSetters can be set to true, then <see cref="SingleThreadedAttribute"/> will NOT be applied on property setters.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public class SingleThreadedClassAttribute : MethodLevelAspect, IAspectProvider
    {
        private readonly SingleThreadedClassPolicy policy;

        public bool IgnoreGetters { get; set; }
        public bool IgnoreSetters { get; set; }

        public SingleThreadedClassAttribute(SingleThreadedClassPolicy policy = SingleThreadedClassPolicy.Default)
        {
            this.policy = policy;

            this.IgnoreGetters = true;
            this.IgnoreSetters = false;
        }

        public IEnumerable<AspectInstance> ProvideAspects(object targetElement)
        {
            var method = (MethodBase)targetElement;

            if (method.IsConstructor) yield break;

            if (method.IsSpecialName)
            {
                if (this.IgnoreGetters && method.Name.StartsWith("get_")) yield break;
                if (this.IgnoreSetters && method.Name.StartsWith("set_")) yield break;
            }

            SingleThreadPolicy methodPolicy;
                
            if (method.IsStatic)
            {
                methodPolicy = SingleThreadPolicy.ClassLevel;
            }
            else
            {
                methodPolicy = (this.policy == SingleThreadedClassPolicy.ThreadAffined) ? SingleThreadPolicy.ThreadAffinedInstance : SingleThreadPolicy.NonThreadAffinedInstance;
            }

            yield return new AspectInstance(targetElement, new SingleThreadedAttribute(methodPolicy));
        }
    }

    public enum SingleThreadedClassPolicy
    {
        ThreadAffined,
        NonThreadAffined,
        Default = NonThreadAffined
    }
}