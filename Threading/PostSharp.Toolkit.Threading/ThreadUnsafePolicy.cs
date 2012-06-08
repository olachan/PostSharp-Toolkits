#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

namespace PostSharp.Toolkit.Threading
{
    public enum ThreadUnsafePolicy
    {
        /// <summary>
        /// Instance methods are safe to run concurrently on different instances of the class.
        /// Static method are thread-safe.
        /// </summary>
        Instance,

        /// <summary>
        /// Instance methods are not thread-safe, even on different instances of the class.
        /// Static methods are not thread-safe as well.
        /// </summary>
        Static,

        /// <summary>
        /// Instance methods should only be run in the thread in which the object was created,
        /// static method are thread-safe
        /// </summary>
        ThreadAffined
    }
}