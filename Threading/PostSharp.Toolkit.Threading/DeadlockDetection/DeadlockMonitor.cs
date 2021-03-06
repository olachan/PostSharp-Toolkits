#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ThreadState = System.Threading.ThreadState;

namespace PostSharp.Toolkit.Threading.DeadlockDetection
{
    internal static class DeadlockMonitor
    {
        private static readonly Graph graph = new Graph();
        private static int detectionPending; // 0 - no detection pending, 1 - detection pending, using int to benefit from Interlocked class
        private static readonly WeakHashSet ignoredResources = new WeakHashSet();
        private static readonly ReaderWriterLockSlim ignoredResourcesReaderWriterLock = new ReaderWriterLockSlim( LockRecursionPolicy.SupportsRecursion );
        private static bool disableDeadlockDetection;

        /// <summary>Method to be invoked before starting to wait for a synchronization object.</summary>
        /// <param name="syncObject">The synchronization object that will be waited for.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        public static void EnterWaiting( object syncObject, ResourceType syncObjectRole )
        {
            AddEdge( Thread.CurrentThread, ResourceType.Thread, syncObject, syncObjectRole );
        }

        /// <summary>Method to be invoked after waiting for a synchronization object, typically when the object
        /// has not been acquired.</summary>
        /// <param name="syncObject">The synchronization object that is no longer waited for.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        public static void ExitWaiting( object syncObject, ResourceType syncObjectRole )
        {
            graph.RemoveEdge( Thread.CurrentThread, ResourceType.Thread, syncObject, syncObjectRole );
        }

        /// <summary>Method to be invoked after waiting for a synchronization object,
        /// when it has been acquired.</summary>
        /// <param name="syncObject">The synchronization object that has been acquired.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        public static void ConvertWaitingToAcquired( object syncObject, ResourceType syncObjectRole )
        {
            Thread thread = Thread.CurrentThread;
            graph.RemoveEdge( thread, ResourceType.Thread, syncObject, syncObjectRole );
            AddEdge( syncObject, syncObjectRole, thread, ResourceType.Thread );

            // RandomSleep();
        }

        /// <summary>Method to be invoked after a synchronization object has been acquired, typically
        /// when the object has not been waited for (in this case, methods <see cref="EnterWaiting"/>
        /// and <see cref="ConvertWaitingToAcquired"/> would have been called).</summary>
        /// <param name="syncObject">The synchronization object that has been acquired.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        public static void EnterAcquired( object syncObject, ResourceType syncObjectRole )
        {
            AddEdge( syncObject, syncObjectRole, Thread.CurrentThread, ResourceType.Thread );

            // RandomSleep();
        }

        /// <summary>Method to be invoked after a synchronization object has been released.</summary>
        /// <param name="syncObject">The synchronization object that has been released.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        public static void ExitAcquired( object syncObject, ResourceType syncObjectRole )
        {
            graph.RemoveEdge( syncObject, syncObjectRole, Thread.CurrentThread, ResourceType.Thread );
        }

        /// <summary>
        /// Adds ignored resource. This resource will not be taken to considiration during deadlock detection.
        /// </summary>
        /// <param name="resource"></param>
        public static void IgnoreResource( object resource, ResourceType resourceType )
        {
            ignoredResourcesReaderWriterLock.EnterWriteLock();

            if ( ignoredResources.Count == 0 )
            {
                Debug.Print( "Synchronization resource added to ignored list" );
            }

            try
            {
                if ( ignoredResources.Add( resource ) )
                {
                    graph.RemoveAdjecentEdges( resource, resourceType );
                }

                if ( ignoredResources.Count > 50 )
                {
                    ignoredResources.ClearNotAlive();
                    if ( ignoredResources.Count > 50 )
                    {
                        disableDeadlockDetection = true;
                        Debug.Print( "Deadlock detection disabled because there are too many ignored resources" );
                    }
                }
            }
            finally
            {
                ignoredResourcesReaderWriterLock.ExitWriteLock();
            }
        }

        private static void AddEdge( object from, ResourceType fromType, object to, ResourceType toType )
        {
            ignoredResourcesReaderWriterLock.EnterReadLock();
            try
            {
                if ( ignoredResources.Contains( from ) || ignoredResources.Contains( to ) )
                {
                    return;
                }

                graph.AddEdge( from, fromType, to, toType );
            }
            finally
            {
                ignoredResourcesReaderWriterLock.ExitReadLock();
            }
        }


        /// <summary>
        /// Detects deadlocks by analyzing the graph of wait dependencies.
        /// </summary>
        /// <param name="startThread">When not null deadlock detection starts from given thread and is conducted noly in graph component containing passed thread</param>
        /// <remarks>
        /// 	<para>The algorithm works by analyzing cycles in the dependency graph. Cycles
        ///     indicate potential deadlocks, and their may be situations where a cycle does not
        ///     correspond to a deadlock.</para>
        /// 	<para>
        ///         Indeed, and although the dependency graph is updated in real time, it may not
        ///         always correspond to the reality. For instance, a deadlock detection may occur
        ///         while some thread is not waiting, for instance after the invocation of a
        ///         <strong>Wait</strong> method and before the invocation of the 
        ///         <see cref="ConvertWaitingToAcquired"/> method.
        ///     </para>
        /// 	<para>To make sure that all threads are waiting, we do the following when we detect
        ///     a cycle in the dependency graph:</para>
        /// 	<list type="bullet">
        /// 		<item></item>
        /// 		<item>We require every edge involved in a cycle to be at least 50 ms
        ///         old.</item>
        /// 		<item>We require every thread involved in a cycle to be in waiting
        ///         state.</item>
        /// 	</list>
        /// 	<para class="xmldocbulletlist">If these conditions are not met, we restart the
        ///     detection (after sleeping some time, in the first case).</para>
        /// 	<para class="xmldocbulletlist">
        ///         When a cycle is detected and identified as a deadlock, we produce an exhaustive
        ///         report of the deadlock cycle, including the stack trace of all threads
        ///         involved. Then, we interrupt each thread invoked using the <see cref="Thread.Interrupt"/> method; 
        ///         we throw a <see cref="DeadlockException"/> in the current thread.
        ///     </para>
        /// 	<para class="xmldocbulletlist">When a deadlock is detected, the application is
        ///     expected to stop, since it may have been left in an inconsistent state.</para>
        /// </remarks>
        public static void DetectDeadlocks( Thread startThread = null )
        {
            if ( disableDeadlockDetection )
            {
                throw new DeadlockDetectionDisabledException();
            }

            DetectDeadlocksInternal( startThread );
        }

        internal static void DetectDeadlocksInternal( Thread startThread = null )
        {
            if ( disableDeadlockDetection )
            {
                Debug.Print( "Deadlock detection canceled because there are too many ignored resources" );
                return;
            }

            if ( Interlocked.CompareExchange( ref detectionPending, 1, 0 ) == 1 )
            {
                Debug.Print( "Deadlock detection skipped because another one is pending." );
                return;
            }

            try
            {
                Debug.Print( "Deadlock detection started in thread {0}.", Thread.CurrentThread.ManagedThreadId );

                IEnumerable<Edge> cycle;

                bool deadlockDetected = startThread != null
                                            ? graph.DetectCycles( startThread, ResourceType.Thread, out cycle )
                                            : graph.DetectCycles( out cycle );

                if ( deadlockDetected )
                {
                    ThrowDeadlockException( cycle );
                }

                Debug.Print( "No cycle detected." );
            }
            finally
            {
                detectionPending = 0;
            }
        }

        /// <summary>
        /// Execute action nad handle possible ThreadAbortException. If thread is aborted because of deadlock the thread abort exception is replaced by DeadlockException.
        /// </summary>
        /// <param name="action"></param>
        internal static void ExecuteAction( Action action )
        {
            try
            {
                action();
            }
            catch ( ThreadAbortException e )
            {
                ThreadAbortToken stateInfo = e.ExceptionState as ThreadAbortToken;
                if ( stateInfo == null )
                {
                    throw;
                }

                Thread.ResetAbort();
                throw new DeadlockException( stateInfo.Message );
            }
        }

        private static void ThrowDeadlockException( IEnumerable<Edge> cycle )
        {
            // We found a cycle. Analyze it to produce a meaningful error message.
            Debug.Print( "Found a cycle in thread dependencies." );

            StringBuilder messageBuilder = new StringBuilder( "Deadlock detected. The following synchronization elements form a cycle: " );

            int i = 0;

            foreach ( Edge edge in cycle )
            {
                Debug.Print( "In cycle: {0}.", edge );
                messageBuilder.AppendFormat( "#{1}={{{0}}}", edge.Successor, i );
            }


            AbortThreadsInDedlockAndEmitStackTraces( cycle, messageBuilder );

            throw new DeadlockException( messageBuilder.ToString() );
        }

        private static void AbortThreadsInDedlockAndEmitStackTraces( IEnumerable<Edge> cycle, StringBuilder messageBuilder )
        {
            IEnumerable<Thread> threadsInDeadlock =
                cycle.Where( x => x.Predecessor.Role == ResourceType.Thread ).Select( x => x.Predecessor.SyncObject as Thread );
            List<Thread> suspendedThreads = new List<Thread>();

            // susspend all threads in deadlock
            foreach ( Thread thread in threadsInDeadlock )
            {
                if ( thread != Thread.CurrentThread )
                {
#pragma warning disable 612,618
                    thread.Suspend();
#pragma warning restore 612,618
                    suspendedThreads.Add( thread );
                }
            }

            // collect stack traces of all suspended threads
            foreach ( Thread thread in suspendedThreads )
            {
                messageBuilder.AppendFormat(
                    Environment.NewLine + Environment.NewLine +
                    "-- start of stack trace of thread {0} (Name=\"{1}\"):" + Environment.NewLine,
                    thread.ManagedThreadId,
                    thread.Name );
                try
                {
                    StackTrace stackTrace = new StackTrace( thread, true );
                    messageBuilder.Append( stackTrace.ToString() );
                }
                catch ( Exception e )
                {
                    messageBuilder.Append( "Cannot get a stack trace: " );
                    messageBuilder.Append( e.Message );
                }

                messageBuilder.AppendFormat(
                    Environment.NewLine + "-- end of stack trace of thread {0}", thread.ManagedThreadId );
            }

            messageBuilder.AppendFormat(
                Environment.NewLine + Environment.NewLine + "-- current thread is {0} (Name=\"{1}\")",
                Thread.CurrentThread.ManagedThreadId,
                Thread.CurrentThread.Name );

            string message = messageBuilder.ToString();

            // abrot threads in deadlock
            foreach ( Thread thread in threadsInDeadlock )
            {
                if ( thread != Thread.CurrentThread )
                {
                    try
                    {
                        thread.Abort( new ThreadAbortToken( message ) );
                    }
                    catch ( ThreadStateException ) // The thread's suspended - we do know it
                    {
                        if ( !thread.ThreadState.HasFlag( ThreadState.AbortRequested ) )
                        {
                            throw;
                        }
                    }
#pragma warning disable 612,618
                    thread.Resume();
#pragma warning restore 612,618
                }
            }
        }
    }
}