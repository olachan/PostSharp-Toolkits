using System;

namespace PostSharp.Toolkit.Threading
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public class ThreadUnsafeMethodAttribute : Attribute
    {
    }
}
