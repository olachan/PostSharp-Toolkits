using System;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Custom attribute specifying that marked Property can be automatically  analyzed notify property changed mechanism. All calls that cannot be analyzed will be ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotifyPropertyChangedSafeAttribute : Attribute
    {
    }
}