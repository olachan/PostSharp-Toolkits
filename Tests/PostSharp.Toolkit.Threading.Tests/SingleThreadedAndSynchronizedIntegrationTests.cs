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
using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class SingleThreadedAndSynchronizedIntegrationTests
    {
        protected void InvokeSimultaneouslyAndWait( Action action1, Action action2 )
        {
            try
            {
                Task t1 = new Task( action1 );
                Task t2 = new Task( action2 );
                t1.Start();
                t2.Start();
                Task.WaitAll( new[] {t1, t2} );
            }
            catch ( AggregateException aggregateException )
            {
                Thread.Sleep( 200 ); //Make sure the second running task is over as well
                if ( aggregateException.InnerExceptions.Count == 1 )
                {
                    throw aggregateException.InnerException;
                }
                throw;
            }
        }

        protected long InvokeAndTraceTime( Action action )
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        [Test]
        public void MethodsInvokedOnSeparateObjects_NoException()
        {
            ThreadUnsafeClass o1 = new ThreadUnsafeClass();
            ThreadUnsafeClass o2 = new ThreadUnsafeClass();
            InvokeSimultaneouslyAndWait( o1.SingleThreadedInstanceDependentMethod, o2.SingleThreadedInstanceDependentMethod );
        }

        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
        public void SameInstanceDependentMethodInvokedTwice_Exception()
        {
            ThreadUnsafeClass o1 = new ThreadUnsafeClass();
            InvokeSimultaneouslyAndWait( o1.SingleThreadedInstanceDependentMethod, o1.SingleThreadedInstanceDependentMethod );
        }

//
//        [Test]
//        public void SameInstanceDependentSingleThreadAndSyncMethodInvoked_NoException()
//        {
//            var o1 = new SingleThreadedAndSynchronizedMethodsObject();
//            InvokeSimultaneouslyAndWait(o1.SingleThreadedInstanceDependentMethod, o1.SynchronizedInstanceDependentMethod);
//        }

        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
        public void TypeDependentStaticMethodInvokedTwice_Exception()
        {
            InvokeSimultaneouslyAndWait( ThreadUnsafeStaticClass.SingleThreadedStaticTypeDependentMethod,
                                         ThreadUnsafeStaticClass.SingleThreadedStaticTypeDependentMethod );
        }

//
//        [Test]
//        public void SameInstanceIndependentMethodInvokedTwice_Exception()
//        {
//            var time = this.InvokeAndTraceTime(() =>
//               {
//                   var o1 = new SynchronizedClass();
//                   InvokeSimultaneouslyAndWait(o1.SynchronizedInstanceDependentMethod, o1.SynchronizedInstanceDependentMethod2);
//               });
//
//            Assert.GreaterOrEqual(time, 400);
//        }
    }

    [ThreadUnsafeObject]
    public class ThreadUnsafeClass
    {
        public void SingleThreadedInstanceDependentMethod()
        {
            Thread.Sleep( 200 );
        }

        public void SingleThreadedInstanceDependentMethod2()
        {
            Thread.Sleep( 200 );
        }
    }

    [ThreadUnsafeObject( ThreadUnsafePolicy.Static )]
    public static class ThreadUnsafeStaticClass
    {
        public static void SingleThreadedStaticTypeDependentMethod()
        {
            Thread.Sleep( 200 );
        }

        public static void SingleThreadedStaticTypeDependentMethod2()
        {
            Thread.Sleep( 200 );
        }
    }

//
//    public class SynchronizedClass
//    {
//        [Synchronized]
//        public static void SynchronizedStaticTypeDependentMethod()
//        {
//            Thread.Sleep(200);
//        }
//
//        [Synchronized]
//        public static void SynchronizedStaticTypeDependentMethod2()
//        {
//            Thread.Sleep(200);
//        }
//
//        [Synchronized]
//        public void SynchronizedInstanceDependentMethod()
//        {
//            Thread.Sleep(200);
//        }
//
//        [Synchronized]
//        public void SynchronizedInstanceDependentMethod2()
//        {
//            Thread.Sleep(200);
//        }
//
//    }
}