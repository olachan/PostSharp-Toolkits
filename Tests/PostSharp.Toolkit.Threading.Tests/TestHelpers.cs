#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    public static class TestHelpers
    {
        public static void InvokeSimultaneouslyAndWait( Action action1, Action action2 )
        {
            Task t1 = new Task(action1);
            Task t2 = new Task(action2);

            var tasks = new[] { t1, t2 };

            try
            {
               
                t1.Start();
                t2.Start();
                Assert.IsTrue( Task.WaitAll( new[] {t1, t2}, 2000 ), "Task wait timed out" );
            }
            catch ( AggregateException aggregateException )
            {
                throw aggregateException.InnerException;
            }

            var ex = tasks.Select(t => t.Exception).FirstOrDefault(e => e != null);

            if (ex != null)
            {
                throw ex.InnerException;
            }
        }

        public static void InvokeSimultaneouslyAndWaitForDeadlockDetection( Action action1, Action action2, int timeout = Timeout.Infinite )
        {
            Exception firstException = null;
            Task t1 = new Task( action1 );
            Task t2 = new Task( action2 );
            t1.Start();
            t2.Start();
            try
            {
                t1.Wait( timeout );
            }
            catch ( AggregateException aggregateException )
            {
                if ( !(aggregateException.InnerExceptions.Count == 1 && aggregateException.InnerException is DeadlockException) )
                {
                    t2.Wait( timeout );
                    throw;
                }
                firstException = aggregateException.InnerException;
            }

            try
            {
                t2.Wait( timeout );
            }
            catch ( AggregateException aggregateException )
            {
                if ( !(aggregateException.InnerExceptions.Count == 1 && aggregateException.InnerException is DeadlockException) )
                {
                    throw;
                }
            }

            if ( firstException != null )
            {
                throw firstException;
            }
        }

        public static void Swallow<TException>( Action action )
            where TException : Exception
        {
            try
            {
                action();
            }
            catch ( TException exc )
            {
                Debug.Print( "Exception {0} swallowed from thread {1}", exc.Message, Thread.CurrentThread.ManagedThreadId );
                //Swallow
            }
        }
    }
}