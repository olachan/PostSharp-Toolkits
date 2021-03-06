#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Custom attribute specifying that marked Property/Method should not be part of automatic property change notification mechanism.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Method )]
    public class NotifyPropertyChangedIgnoreAttribute : Attribute
    {
    }
}