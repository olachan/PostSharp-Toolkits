using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using PostSharp.Toolkit.Threading.ReaderWriter;

using ThreadState = System.Threading.ThreadState;

namespace PostSharp.Toolkit.Threading.Deadlock
{
    /// <summary>
    /// Detects deadlocks occurring because of circular wait conditions.
    /// </summary>
    /// <remarks>
    /// 	<para>
    ///         The <see cref="DeadlockMonitor"/> works by building, in real time, a graph
    ///         of dependencies between threads and waiting objects. Waiting objects have to
    ///         cooperate with the <see cref="DeadlockMonitor"/> to add and remove edges
    ///         to the graph. Currently, the only synchronization objects cooperating with
    ///         <see cref="DeadlockMonitor"/> are <see cref="ReaderWriterLockWrapper"/> and 
    ///         <b>WpfDispatchAttribute</b>. Therefore,
    ///         deadlocks occurring because of other objects will not be detected.
    ///     </para>
    /// 	<para>
    ///         Synchronization objects update the wait graph by calling methods <see cref="EnterWaiting"/> 
    ///         (before starting to wait), <see cref="ConvertWaitingToAcquired"/> (when a synchronization object has been
    ///         acquired after waiting), <see cref="EnterAcquired"/> (when a
    ///         synchronization object has been acquired without waiting), <see cref="ExitWaiting"/> 
    ///         (after waiting, when the synchronization object has not
    ///         been acquired), or <see cref="ExitAcquired"/> (after the synchronization
    ///         object has been released).
    ///     </para>
    /// 	<para>
    ///         Synchronization objects are expected to wait only a defined amount of time.
    ///         When this amount of time has elapsed, they should cause a deadlock detection
    ///         by invoking the <see cref="DetectDeadlocks"/> methods. This method will
    ///         analyze the dependency graph for cycles and throw a <see cref="DeadlockException"/> if 
    ///         a deadlock is detected. Additionally, all threads
    ///         that participate in the deadlock will be interrupted using <see cref="Thread.Interrupt"/>.
    ///     </para>
    /// </remarks>
    /// <notes>
    ///     Synchronization objects can have locks of many roles. For instance, a 
    ///     <see cref="ReaderWriterLockSlim"/> object has to be represented with three roles:
    ///     <em>read</em>, <em>write</em> and <em>upgradeable read</em>. Individual roles of
    ///     synchronization objects are considered as separate nodes in the dependency graph.
    /// </notes>
    public static class DeadlockMonitor
    {
        private static readonly Graph graph = new Graph();
        private static bool detectionPending;

        /// <summary>Method to be invoked before starting to wait for a synchronization object.</summary>
        /// <param name="syncObject">The synchronization object that will be waited for.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        /// <param name="syncObjectInfo">
        ///     Object representing <paramref name="syncObject"/> when human-readable information
        ///     is displayed (the <see cref="object.ToString"/> method of this object is used), or
        ///     <strong>null</strong>
        /// </param>
        [Conditional( "DEBUG" )]
        public static void EnterWaiting( object syncObject, ResourceType syncObjectRole, object syncObjectInfo )
        {
            lock (graph)
            {
                graph.AddEdge(Thread.CurrentThread, null, ResourceType.Thread, syncObject, syncObjectInfo, syncObjectRole);

                if (syncObjectRole == ResourceType.Write)
                {
                    graph.AddEdge(Thread.CurrentThread, null, ResourceType.Thread, syncObject, syncObjectInfo, ResourceType.Read);
                }
            }
        }

        /// <summary>Method to be invoked after waiting for a synchronization object, typically when the object
        /// has not been acquired.</summary>
        /// <param name="syncObject">The synchronization object that is no longer waited for.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        [Conditional( "DEBUG" )]
        public static void ExitWaiting(object syncObject, ResourceType syncObjectRole)
        {
            lock (graph)
            {
                graph.RemoveEdge(Thread.CurrentThread, ResourceType.Thread, syncObject, syncObjectRole);

                if (syncObjectRole == ResourceType.Write)
                {
                    graph.RemoveEdge(Thread.CurrentThread, ResourceType.Thread, syncObject, ResourceType.Read);
                }
            }
        }

        /// <summary>Method to be invoked after waiting for a synchronization object,
        /// when it has been acquired.</summary>
        /// <param name="syncObject">The synchronization object that has been acquired.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        /// <param name="syncObjectInfo">
        ///     Object representing <paramref name="syncObject"/> when human-readable information
        ///     is displayed (the <see cref="object.ToString"/> method of this object is used), or
        ///     <strong>null</strong>
        /// </param>
        [Conditional( "DEBUG" )]
        public static void ConvertWaitingToAcquired(object syncObject, ResourceType syncObjectRole, object syncObjectInfo)
        {
            Thread thread = Thread.CurrentThread;
            lock (graph)
            {
                graph.RemoveEdge(thread, ResourceType.Thread, syncObject, syncObjectRole);
                graph.AddEdge(syncObject, syncObjectInfo, syncObjectRole, thread, null, ResourceType.Thread);

                if (syncObjectRole == ResourceType.Write)
                {
                    graph.RemoveEdge(thread, ResourceType.Thread, syncObject, ResourceType.Read);
                    graph.AddEdge(syncObject, syncObjectInfo, ResourceType.Read, thread, null, ResourceType.Thread);
                }
            }

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
        /// <param name="syncObjectInfo">
        ///     Object representing <paramref name="syncObject"/> when human-readable information
        ///     is displayed (the <see cref="object.ToString"/> method of this object is used), or
        ///     <strong>null</strong>
        /// </param>
        [Conditional( "DEBUG" )]
        public static void EnterAcquired( object syncObject, ResourceType syncObjectRole, object syncObjectInfo )
        {
            lock (graph)
            {
                graph.AddEdge(syncObject, syncObjectInfo, syncObjectRole, Thread.CurrentThread, null, ResourceType.Thread);

                if (syncObjectRole == ResourceType.Write)
                {
                    graph.AddEdge(syncObject, syncObjectInfo, ResourceType.Read, Thread.CurrentThread, null, ResourceType.Thread);
                }
            }

            // RandomSleep();
        }

        /// <summary>Method to be invoked after a synchronization object has been released.</summary>
        /// <param name="syncObject">The synchronization object that has been released.</param>
        /// <param name="syncObjectRole">
        ///     Role (or name) of the lock, inside <paramref name="syncObject"/>, or
        ///     <strong>null</strong> if the synchronization object has no role.
        /// </param>
        [Conditional( "DEBUG" )]
        public static void ExitAcquired( object syncObject, ResourceType syncObjectRole )
        {
            lock (graph)
            {
                graph.RemoveEdge(syncObject, syncObjectRole, Thread.CurrentThread, ResourceType.Thread);
                
                if (syncObjectRole == ResourceType.Write)
                {
                    graph.RemoveEdge(syncObject, ResourceType.Read, Thread.CurrentThread, ResourceType.Thread);
                }
            }
        }

        /// <summary>
        /// Detects deadlocks by analyzing the graph of wait dependencies.
        /// </summary>
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
        [Conditional( "DEBUG" )]
        public static void DetectDeadlocks()
        {
            if ( detectionPending )
            {
                Debug.Print( "Deadlock detection skipped because another one is pending." );
                return;
            }

            detectionPending = true;

            Debug.Print( "Deadlock detection started." );

            try
            {
                IList<Edge> cycle;

                if (graph.DetectCycles(out cycle))
                {
                    ThrowDeadlockException(cycle);
                }

                Debug.Print( "No cycle detected." );
            }
            finally
            {
                detectionPending = false;
            }
        }

        private static void ThrowDeadlockException(IList<Edge> cycle)
        {
            // We found a cycle. Analyze it to produce a meaningful error message.
            Debug.Print("Found a cycle in thread dependencies.");

            StringBuilder messageBuilder = new StringBuilder("Deadlock detected. The following synchronization elements form a cycle: ");

            int i = 0;

            foreach (var edge in cycle)
            {
                Debug.Print("In cycle: {0}.", edge);
                messageBuilder.AppendFormat("#{1}={{{0}}}", edge.Successor.Format(edge.SuccessorInfo), i);
            }


            EmitStackTraces(cycle, messageBuilder);

            throw new DeadlockException(messageBuilder.ToString());
        }

        private static void EmitStackTraces(IList<Edge> cycle, StringBuilder messageBuilder)
        {
            foreach (var thread in cycle.Where(x => x.Predecessor.Role == ResourceType.Thread).Select(x => x.Predecessor.SyncObject as Thread))
            {
                if (thread != Thread.CurrentThread)
                {
                    messageBuilder.AppendFormat(
                        Environment.NewLine + Environment.NewLine +
                        "-- start of stack trace of thread {0} (Name=\"{1}\"):" + Environment.NewLine,
                        thread.ManagedThreadId,
                        thread.Name);
                    
                    try
                    {
#pragma warning disable 612,618
                        thread.Suspend();
                        StackTrace stackTrace = new StackTrace(thread, true);
                        messageBuilder.Append(stackTrace.ToString());
                        thread.Resume();
                        thread.Interrupt();
#pragma warning restore 612,618
                    }
                    catch (Exception e)
                    {
                        messageBuilder.Append("Cannot get a stack trace: ");
                        messageBuilder.Append(e.Message);
                    }

                    messageBuilder.AppendFormat(
                        Environment.NewLine + "-- end of stack trace of thread {0}", thread.ManagedThreadId);
                }
                else
                {
                    messageBuilder.AppendFormat(
                        Environment.NewLine + Environment.NewLine + "-- current thread is {0} (Name=\"{1}\")",
                        thread.ManagedThreadId,
                        thread.Name);
                }
            }
        }
    }
}
