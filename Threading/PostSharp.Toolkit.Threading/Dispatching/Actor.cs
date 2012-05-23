#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    [Actor( AttributeInheritance = MulticastInheritance.Strict )]
    public abstract class Actor : IDispatcherObject, IDispatcher
    {
        // TODO: Compatibility with async/await: methods returning a Task should be handled properly.

        private readonly ConcurrentQueue<IAction> workItems = new ConcurrentQueue<IAction>();
        private volatile Thread currentThread;
        private int workItemsCount;


        private void ProcessQueue()
        {
            // Avoid concurrent execution.
            if ( Interlocked.CompareExchange( ref currentThread, Thread.CurrentThread, null ) != null )
                return;

            try
            {
                IAction action;
                while ( workItems.TryDequeue( out action ) )
                {
                    // TODO: Cooperative multitasking: Avoid processing the whole queue if it's very long.
                    // Rather interrupt and requeue a ProcessQueue task.

                    try
                    {
                        action.Invoke();
                    }
                    catch ( Exception e )
                    {
                        bool handled = false;
                        this.OnException( e, ref handled );
                        if ( !handled )
                            throw;
                    }
                    finally
                    {
                        Interlocked.Decrement( ref workItemsCount );
                    }
                }
            }
            finally
            {
                currentThread = null;
            }
        }

        IDispatcher IDispatcherObject.Dispatcher
        {
            get { return this; }
        }

        bool IDispatcher.CheckAccess()
        {
            return this.currentThread == Thread.CurrentThread;
        }

        void IDispatcher.Invoke( IAction action )
        {
            throw new NotSupportedException();
        }

        void IDispatcher.BeginInvoke( IAction action )
        {
            if ( this.IsDisposed ) throw new ObjectDisposedException( this.ToString() );
            workItems.Enqueue( action );

            if ( Interlocked.Increment( ref this.workItemsCount ) == 1 )
                new Task( ProcessQueue ).Start();
        }

        protected virtual void OnException( Exception exception, ref bool handled )
        {
        }


        public bool IsDisposed { [ThreadSafe]
        get; private set; }

        [ThreadSafe]
        public virtual void Dispose()
        {
            this.IsDisposed = true;
        }
    }
}