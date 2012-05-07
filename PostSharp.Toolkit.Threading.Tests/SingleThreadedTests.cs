using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Threading;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class SingleThreadedTests
    {
        protected void InvokeSimultaneouslyAndWait(Action action1, Action action2)
        {
            try
            {
                var t1 = new Task(action1);
                var t2 = new Task(action2);
                t1.Start();
                t2.Start();
                t1.Wait();
                t2.Wait();
            }
            catch (AggregateException aggregateException)
            {
                Thread.Sleep(500); //Make sure the second running task is over as well
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    throw aggregateException.InnerException;
                }
                throw;
            }
        }

        [Test]
        public void TwoInstanceIndependentMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedEntity();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod2);
        }

        [Test]
        public void InstanceDependentAndIndependentMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedEntity();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceDependentMethod);
        }

        [Test]
        public void MethodsInvokedOnSeparateObjects_NoException()
        {
            var o1 = new SingleThreadedEntity();
            var o2 = new SingleThreadedEntity();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o2.InstanceDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void SameInstanceIndependentMethodInvokedTwice_Exception()
        {
            var o1 = new SingleThreadedEntity();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void SameInstanceDependentMethodInvokedTwice_Exception()
        {
            var o1 = new SingleThreadedEntity();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o1.InstanceDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void TwoInstanceDependentMethodsInvoked_Exception()
        {
            var o1 = new SingleThreadedEntity();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o1.InstanceDependentMethod2);
        }

        [Test]
        public void TypeIndependentAndDependentStaticMethodsInvoked_NoException()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedEntity.StaticIndependentMethod,
                                        SingleThreadedEntity.StaticTypeDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void TypeIndependentStaticMethodInvokedTwice_Exception()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedEntity.StaticIndependentMethod,
                                        SingleThreadedEntity.StaticIndependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void TypeDependentStaticMethodInvokedTwice_Exception()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedEntity.StaticTypeDependentMethod,
                                        SingleThreadedEntity.StaticTypeDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void TwoTypeDependentStaticMethodInvoked_Exception()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedEntity.StaticTypeDependentMethod,
                                        SingleThreadedEntity.StaticTypeDependentMethod2);
        }

        [Test]
        public void MethodThrowsException_MonitorProperlyReleased()
        {
            var o1 = new SingleThreadedEntity();
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(o1.Exception),
                                        () => { Thread.Sleep(500); Swallow<NotSupportedException>(o1.Exception); });
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(o1.Exception),
                                        () => { Thread.Sleep(500); Swallow<NotSupportedException>(o1.InstanceDependentMethod); });
        }

        [Test]
        public void StaticMethodThrowsException_MonitorProperlyReleased()
        {
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(SingleThreadedEntity.StaticException),
                                        () => { Thread.Sleep(500); Swallow<NotSupportedException>(SingleThreadedEntity.StaticException); });
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(SingleThreadedEntity.StaticException),
                                        () => { Thread.Sleep(500); Swallow<NotSupportedException>(SingleThreadedEntity.StaticTypeDependentMethod); });
        }

        protected void Swallow<TException>(Action action)
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


        public class SingleThreadedEntity
        {
            [SingleThreaded(false)]
            public static void StaticIndependentMethod()
            {
                Thread.Sleep(500);
            }

            [SingleThreaded(true)]
            public static void StaticTypeDependentMethod()
            {
                Thread.Sleep(500);
            }

            [SingleThreaded(true)]
            public static void StaticTypeDependentMethod2()
            {
                Thread.Sleep(500);
            }

            [SingleThreaded(false)]
            public void InstanceIndependentMethod()
            {
                Thread.Sleep(500);
            }

            [SingleThreaded(false)]
            public void InstanceIndependentMethod2()
            {
                Thread.Sleep(500);
            }

            [SingleThreaded(true)]
            public void InstanceDependentMethod()
            {
                Thread.Sleep(500);
            }

            [SingleThreaded(true)]
            public void InstanceDependentMethod2()
            {
                Thread.Sleep(500);
            }

            [SingleThreaded]
            public void Exception()
            {
                throw new NotSupportedException();
            }

            [SingleThreaded]
            public static void StaticException()
            {
                throw new NotSupportedException();
            }
        }
    }
}