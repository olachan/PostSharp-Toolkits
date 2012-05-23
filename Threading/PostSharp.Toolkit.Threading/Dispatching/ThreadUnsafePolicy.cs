#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

namespace PostSharp.Toolkit.Threading.Dispatching
{
    public enum ThreadUnsafePolicy
    {
        /// <summary>
        /// Instance methods are safe to run concurrently on different instances of the class.
        /// Static method are thread-safe.
        /// </summary>
        Instance,

        /// <summary>
        /// 
        /// </summary>
        Static
    }
}