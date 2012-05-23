#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Threading;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    /// <summary>
    /// Interface to be implemented by classes whose instances are synchronized by
    /// a <see cref="ReaderWriterLockSlim"/>.
    /// </summary>
    public interface IReaderWriterSynchronized
    {
        /// <summary>
        /// Gets the <see cref="ReaderWriterLockSlim"/> that has to be used
        /// to synchronize access to the current instance.
        /// </summary>
        ReaderWriterLockSlim Lock { get; }
    }
}