using System;

namespace PostSharp.Toolkit.Domain
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NotifyPropertyChangedSafeAttribute : Attribute
    {
    }
}