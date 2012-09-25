using System;

namespace PostSharp.Toolkit.Domain
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ChangeTrackingIgnoreField : Attribute
    {
    }
}