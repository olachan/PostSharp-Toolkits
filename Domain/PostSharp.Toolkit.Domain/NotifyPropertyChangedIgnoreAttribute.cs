using System;

namespace PostSharp.Toolkit.Domain
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class NotifyPropertyChangedIgnoreAttribute : Attribute
    {
    }
}