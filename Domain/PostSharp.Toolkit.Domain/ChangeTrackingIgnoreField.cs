using System;

namespace PostSharp.Toolkit.Domain
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class ChangeTrackingIgnoreField : Attribute
    {
    }
}