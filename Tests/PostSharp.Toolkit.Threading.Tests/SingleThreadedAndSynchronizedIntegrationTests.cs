using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using PostSharp.Toolkit.Threading.Dispatching;
using PostSharp.Toolkit.Threading.Synchronization;


namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class SingleThreadedAndSynchronizedIntegrationTests
    {
        protected void InvokeSimultaneouslyAndWait(Action action1, Action action2)
        {
            try
            {
                var t1 = new Task(action1);
                var t2 = new Task(action2);
                t1.Start();
                t2.Start();
                Task.WaitAll(new[] { t1, t2 });
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

        protected long InvokeAndTraceTime(Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        [Test]
        public void MethodsInvokedOnSeparateObjects_NoException()
        {
            var o1 = new ThreadUnsafeClass();
            var o2 = new ThreadUnsafeClass();
            InvokeSimultaneouslyAndWait(o1.SingleThreadedInstanceDependentMethod, o2.SingleThreadedInstanceDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void SameInstanceDependentMethodInvokedTwice_Exception()
        {
            var o1 = new ThreadUnsafeClass();
            InvokeSimultaneouslyAndWait(o1.SingleThreadedInstanceDependentMethod, o1.SingleThreadedInstanceDependentMethod);
        }
//
//        [Test]
//        public void SameInstanceDependentSingleThreadAndSyncMethodInvoked_NoException()
//        {
//            var o1 = new SingleThreadedAndSynchronizedMethodsObject();
//            InvokeSimultaneouslyAndWait(o1.SingleThreadedInstanceDependentMethod, o1.SynchronizedInstanceDependentMethod);
//        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void TypeDependentStaticMethodInvokedTwice_Exception()
        {
            InvokeSimultaneouslyAndWait(ThreadUnsafeStaticClass.SingleThreadedStaticTypeDependentMethod,
                                        ThreadUnsafeStaticClass.SingleThreadedStaticTypeDependentMethod);
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

    [ThreadUnsafeClass]
    public class ThreadUnsafeClass
    {
    
        public void SingleThreadedInstanceDependentMethod()
        {
            Thread.Sleep(200);
        }

        public void SingleThreadedInstanceDependentMethod2()
        {
            Thread.Sleep(200);
        }
        
    }

    [ThreadUnsafeClass(ThreadUnsafePolicy.Static)]
    public static class ThreadUnsafeStaticClass
    {
        public static void SingleThreadedStaticTypeDependentMethod()
        {
            Thread.Sleep(200);
        }

        public static void SingleThreadedStaticTypeDependentMethod2()
        {
            Thread.Sleep(200);
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

