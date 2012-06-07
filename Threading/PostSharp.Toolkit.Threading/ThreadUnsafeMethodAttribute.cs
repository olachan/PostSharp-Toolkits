#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Custom attribute that, when applied to a method or field runs checks made by <see cref="ThreadUnsafeObjectAttribute"/> even if target of the attribute is private or protected.
    /// </summary>
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Field )]
    public class ThreadUnsafeMethodAttribute : Attribute
    {
    }
}