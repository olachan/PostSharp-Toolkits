using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChangeTrackingIgnoreField : Attribute
    {
    }
}