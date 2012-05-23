#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PostSharp.Toolkit.Threading.DeadlockDetection;

namespace PostSharp.Toolkit.Threading.Tests
{
    public static class TestHelpers
    {
        public static void InvokeSimultaneouslyAndWait( Action action1, Action action2, int timeout = Timeout.Infinite )
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