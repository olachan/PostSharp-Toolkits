using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PostSharp.Toolkit.Domain
{
    //TODO: Rename!
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class NoAutomaticPropertyChangedNotificationsAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotentMethodAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class InstanceScopedPropertyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DependsOn : Attribute
    {
        public string[] Dependencies { get; private set; }

        public DependsOn(params string[] dependencies)
        {
            this.Dependencies = dependencies.ToArray();
        }
    }
}
