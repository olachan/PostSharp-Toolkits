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

#pragma warning disable 420

namespace PostSharp.Toolkit.Threading.Dispatching
{
    [Actor(AttributeInheritance = MulticastInheritance.Strict)]
    public abstract class Actor : IDispatcherObject, IDispatcher
    {
        // TODO: Compatibility with async/await: methods returning a Task should be handled properly.

        private readonly ConcurrentQueue<IAction> workItems;
        private volatile Thread currentThread;
        private volatile int workItemsCount;
        private readonly Actor master;

        protected Actor()
            : this(null)
        {
        }

        protected Actor(Actor master)
        {
            if (master == null)
            {
                this.master = this;
                this.workItems = new ConcurrentQueue<IAction>();
            }
            else
            {
                this.master = master;
            }
        }

        private void ProcessQueue()
        {
            // We cannot do a CAS and exit the method is this.currentThread is not null, because this field is set
            // after the this.workItemsCount field is decremented. Field this.workItemsCount, and not this.currentThread,
            // guarantees there is a single running thread.
            this.currentThread = Thread.CurrentThread;
            

            try
            {
                SpinWait spinWait = new SpinWait();
                do
                {
                    IAction action;
                    while (!this.workItems.TryDequeue(out action))
                    {
                        spinWait.SpinOnce();
                    }

                    // TODO: Cooperative multitasking: Avoid processing the whole queue if it's very long.
                    // Rather interrupt and requeue a ProcessQueue task.

                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        bool handled = false;
                        this.OnException(e, ref handled);
                        if (!handled)
                            throw;
                    }

                } while (Interlocked.Decrement(ref this.workItemsCount) > 0);
            }
            finally
            {
                this.currentThread = null;
            }
        }

        IDispatcher IDispatcherObject.Dispatcher
        {
            get { return this.master; }
        }

        bool IDispatcher.CheckAccess()
        {
            return this.currentThread == Thread.CurrentThread;
        }

        void IDispatcher.Invoke(IAction action)
        {
            throw new NotSupportedException();
        }

        void IDispatcher.BeginInvoke(IAction action)
        {
            if (this.IsDisposed) throw new ObjectDisposedException(this.ToString());
            if (this.master != this) throw new InvalidOperationException();

            if (Interlocked.Increment(ref this.workItemsCount) == 1)
            {
                new Task( this.ProcessQueue ).Start();
            }

            this.workItems.Enqueue(action);

           
        }

        protected virtual void OnException(Exception exception, ref bool handled)
        {
        }


        public bool IsDisposed
        {
            [ThreadSafe]
            get;
            private set;
        }

        [ThreadSafe]
        public virtual void Dispose()
        {
            this.IsDisposed = true;
        }
    }
}