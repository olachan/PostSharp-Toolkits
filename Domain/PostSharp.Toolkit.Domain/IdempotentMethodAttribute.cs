using System;

namespace PostSharp.Toolkit.Domain
{
    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotentMethodAttribute : Attribute
    {
    }
}