using System;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Custom attribute specifying that marked Property/Method should not be part of automatic property change notification mechanism.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class NotifyPropertyChangedIgnoreAttribute : Attribute
    {
    }
}