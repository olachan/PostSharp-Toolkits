#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Exception thrown when an attempt to simultaneously access a single-threaded method is detected.
    /// </summary>
    public class ThreadUnsafeException : ThreadingException
    {
        public ThreadUnsafeErrorType ErrorType { get; private set; }

        public ThreadUnsafeException(ThreadUnsafeErrorType errorType)
            : base( errorType == ThreadUnsafeErrorType.InvalidThread ?
                    "An attempt was made to call thread-affined method from another thread"
                    : "An attempt was made to simultaneously access a single-threaded method from multiple threads." )
        {
            ErrorType = errorType;
        }
    }

    public enum ThreadUnsafeErrorType
    {
        /// <summary>
        /// Single-threaded method was simultaneously accessed from multiple threads
        /// </summary>
        SimultaneousAccess,

        /// <summary>
        /// Thread affined method was accessed from another thread
        /// </summary>
        InvalidThread
    }
}