using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PostSharp.Toolkit.Threading.SingleThreaded;

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
                Task.WaitAll(new[] {t1, t2});
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

        [Test]
        public void TwoInstanceIndependentMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod2);
        }

        [Test]
        public void InstanceDependentAndIndependentMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceDependentMethod);
        }

        [Test]
        public void MethodsInvokedOnSeparateObjects_NoException()
        {
            var o1 = new SingleThreadedMethodsObject();
            var o2 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o2.InstanceDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void SameInstanceIndependentMethodInvokedTwice_Exception()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void SameInstanceDependentMethodInvokedTwice_Exception()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o1.InstanceDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void TwoInstanceDependentMethodsInvoked_Exception()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o1.InstanceDependentMethod2);
        }

        [Test]
        public void TypeIndependentAndDependentStaticMethodsInvoked_NoException()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedMethodsObject.StaticIndependentMethod,
                                        SingleThreadedMethodsObject.StaticTypeDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void TypeIndependentStaticMethodInvokedTwice_Exception()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedMethodsObject.StaticIndependentMethod,
                                        SingleThreadedMethodsObject.StaticIndependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void TypeDependentStaticMethodInvokedTwice_Exception()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedMethodsObject.StaticTypeDependentMethod,
                                        SingleThreadedMethodsObject.StaticTypeDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void TwoTypeDependentStaticMethodInvoked_Exception()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedMethodsObject.StaticTypeDependentMethod,
                                        SingleThreadedMethodsObject.StaticTypeDependentMethod2);
        }

        [Test]
        public void MethodThrowsException_MonitorProperlyReleased()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(o1.Exception),
                                        () => { Thread.Sleep(200); Swallow<NotSupportedException>(o1.Exception); });
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(o1.Exception),
                                        () => { Thread.Sleep(200); Swallow<NotSupportedException>(o1.InstanceDependentMethod); });
        }

        [Test]
        public void StaticMethodThrowsException_MonitorProperlyReleased()
        {
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(SingleThreadedMethodsObject.StaticException),
                                        () => { Thread.Sleep(200); Swallow<NotSupportedException>(SingleThreadedMethodsObject.StaticException); });
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(SingleThreadedMethodsObject.StaticException),
                                        () => { Thread.Sleep(200); Swallow<NotSupportedException>(SingleThreadedMethodsObject.StaticTypeDependentMethod); });
        }

        [Test]
        public void SingleThreadedClassGetter_MultipleAccessesDoNotThrow()
        {
            var o = new SingleThreadedClassObject();
            int x;
            InvokeSimultaneouslyAndWait(() => { x = o.TestProperty; },
                                        () => { x = o.TestProperty; });
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void SingleThreadedClassSetter_MultipleAccessesThrow()
        {
            var o = new SingleThreadedClassObject();
            InvokeSimultaneouslyAndWait(() => { o.TestProperty = 3; },
                                        () => { o.TestProperty = 3; });
        }

        [Test]
        public void SingleThreadedClassWithIgnoredSetters_MultipleSetterAccessesDoNotThrow()
        {
            var o = new SingleThreadedClassIngoreSettersObject();
            InvokeSimultaneouslyAndWait(() => { o.TestProperty = 3; },
                                        () => { o.TestProperty = 3; });
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


        [SingleThreadedClass]
        public class SingleThreadedClassObject
        {
            private int _testProperty;
            public int TestProperty
            {
                get
                {
                    Thread.Sleep(200);
                    return _testProperty;
                }
                set
                {
                    Thread.Sleep(200);
                    _testProperty = value;
                }
            }
        }

        [SingleThreadedClass(IgnoreSetters = true)]
        public class SingleThreadedClassIngoreSettersObject
        {
            private int _testProperty;
            public int TestProperty
            {
                get
                {
                    Thread.Sleep(200);
                    return _testProperty;
                }
                set
                {
                    Thread.Sleep(200);
                    _testProperty = value;
                }
            }
        }

        public class SingleThreadedMethodsObject
        {
            [SingleThreaded(false)]
            public static void StaticIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(true)]
            public static void StaticTypeDependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(true)]
            public static void StaticTypeDependentMethod2()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(false)]
            public void InstanceIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(false)]
            public void InstanceIndependentMethod2()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(true)]
            public void InstanceDependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(true)]
            public void InstanceDependentMethod2()
            {
                Thread.Sleep(200);
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