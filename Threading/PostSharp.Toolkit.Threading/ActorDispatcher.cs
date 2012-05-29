using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PostSharp.Toolkit.Threading
{
    public sealed class ActorDispatcher : IDispatcher
    {
        private readonly ConcurrentQueue<IAction> workItems = new ConcurrentQueue<IAction>();
        private volatile Thread currentThread;
        private volatile int workItemsCount;
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
                    // Rather interrupt and requeue a ProcessQueue task. Use Stopwatch

                    action.Invoke();
                   
                } while (Interlocked.Decrement(ref this.workItemsCount) > 0);
            }
            finally
            {
                this.currentThread = null;
            }
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
            if (Interlocked.Increment(ref this.workItemsCount) == 1)
            {
                new Task(this.ProcessQueue).Start();
            }

            this.workItems.Enqueue(action);


        }
    }
}
