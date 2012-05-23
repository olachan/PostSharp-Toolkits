#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

namespace PostSharp.Toolkit.Threading.Dispatching
{
    /// <summary>
    /// Exception thrown when an attempt to simultaneously access a single-threaded method is detected.
    /// </summary>
    public class ThreadUnsafeException : ThreadingException
    {
        public ThreadUnsafeException()
            : base( "An attempt was made to simultaneously access a single-threaded method from multiple threads." )
        {
        }
    }
}