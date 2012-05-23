#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Runtime.Serialization;

namespace PostSharp.Toolkit.Threading.Deadlock
{
    /// <summary>
    /// Exception thrown by the <see cref="DeadlockMonitor"/> class when a deadlock
    /// is detected.
    /// </summary>
    [Serializable]
    public sealed class DeadlockException : ThreadingException
    {
        /// <summary>
        /// Initializes a new <see cref="DeadlockException"/>.
        /// </summary>
        public DeadlockException()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DeadlockException"/> and specifies the message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public DeadlockException( string message ) : base( message )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DeadlockException"/> and specifies the message
        /// and inner <see cref="Exception"/>.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="inner">Inner <see cref="Exception"/>.</param>
        public DeadlockException( string message, Exception inner ) : base( message, inner )
        {
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DeadlockException(
            SerializationInfo info,
            StreamingContext context ) : base( info, context )
        {
        }
    }
}