using System;
using System.Threading;
using System.Threading.Tasks;

namespace PostSharp.Toolkit.Threading.Tests
{
    public static class TestHelpers
    {
        public static void InvokeSimultaneouslyAndWait(Action action1, Action action2, int timeout = Timeout.Infinite)
        {
            try
            {
                var t1 = new Task(() => Swallow<ThreadInterruptedException>(action1));
                var t2 = new Task(() => Swallow<ThreadInterruptedException>(action2));
                t1.Start();
                t2.Start();
                t1.Wait(timeout);
                t2.Wait(timeout);
            }
            catch (AggregateException aggregateException)
            {
                Thread.Sleep(200); //Make sure the second running task is over as well
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    throw aggregateException.InnerException;
                }
                throw;
            }
        }

        public static void Swallow<TException>(Action action)
           where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exc)
            {
                //Swallow
            }
        }
    }
}
