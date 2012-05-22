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

        [Test]
        public void TwoInstanceIndependentMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod2);
        }

        [Test]
        public void TwoInstanceIndependentDerivedMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedMethodsDerivedObject();
            InvokeSimultaneouslyAndWait(o1.DerivedInstanceIndependentMethod, o1.DerivedInstanceIndependentMethod2);
        }

        [Test]
        public void InstanceIndependentDerivedAndOntDerivedMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedMethodsDerivedObject();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.DerivedInstanceIndependentMethod2);
        }

        [Test]
        public void InstanceDependentAndIndependentMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceDependentMethod);
        }

        [Test]
        public void InstanceDependentAndIndependentDerivedMethodsInvoked_NoException()
        {
            var o1 = new SingleThreadedMethodsDerivedObject();
            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.DerivedInstanceDependentMethod);
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
        public void InstanceIndependentDerivedAndNotDerivedMethodInvoked_NoException()
        {
            var o1 = new SingleThreadedMethodsDerivedObject();
            InvokeSimultaneouslyAndWait(o1.DerivedInstanceIndependentMethod, o1.InstanceIndependentMethod);
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void InstanceDependentDerivedAndNotDerivedMethodInvoked_Exception()
        {
            var o1 = new SingleThreadedMethodsDerivedObject();
            InvokeSimultaneouslyAndWait(o1.DerivedInstanceDependentMethod, o1.InstanceDependentMethod);
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
        public void TypeIndependentStaticDerivedMethodAndNotDerivedInvoked_NoException()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedMethodsDerivedObject.DerivedStaticIndependentMethod,
                                        SingleThreadedMethodsDerivedObject.StaticIndependentMethod);
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
            var o = new NonAffinedSingleThreadedClassObject();
            int x;
            InvokeSimultaneouslyAndWait(() => { x = o.TestProperty; },
                                        () => { x = o.TestProperty; });
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void SingleThreadedClassSetter_MultipleAccessesThrow()
        {
            var o = new NonAffinedSingleThreadedClassObject();
            InvokeSimultaneouslyAndWait(() => { o.TestProperty = 3; },
                                        () => { o.TestProperty = 3; });
        }

        [Test]
        public void SingleThreadedClassWithIgnoredSetters_MultipleSetterAccessesDoNotThrow()
        {
            var o = new NonAffinedSingleThreadedClassIngoreSettersObject();
            InvokeSimultaneouslyAndWait(() => { o.TestProperty = 3; },
                                        () => { o.TestProperty = 3; });
        }

        [Test]
        public void AffinedSingleThreadedObject_CallingMethodsFromItsThreadDoesNotThrow()
        {
            var o = new AffinedSingleThreadedObject();
            o.DoNothing1();
            o.DoNothing2();
        }

        [Test]
        [ExpectedException(typeof(SingleThreadedException))]
        public void AffinedSingleThreadedObject_CallingMethodsFromOtherThreadThrows()
        {
            var o = new AffinedSingleThreadedObject();
            try
            {
                Task.Factory.StartNew(o.DoNothing1).Wait();
            }
            catch (AggregateException exc)
            {
                if (exc.InnerExceptions.Count == 1)
                {
                    throw exc.InnerException;
                }
                else
                {
                    throw;
                }
            }
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


        [SingleThreadedClass(SingleThreadedClassPolicy.NonThreadAffined)]
        public class NonAffinedSingleThreadedClassObject
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

        [SingleThreadedClass(SingleThreadedClassPolicy.NonThreadAffined, IgnoreSetters = true)]
        public class NonAffinedSingleThreadedClassIngoreSettersObject
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
            [SingleThreaded(SingleThreadPolicy.MethodLevel)]
            public static void StaticIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.ClassLevel)]
            public static void StaticTypeDependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.ClassLevel)]
            public static void StaticTypeDependentMethod2()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.MethodLevel)]
            public void InstanceIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.MethodLevel)]
            public void InstanceIndependentMethod2()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.NonThreadAffinedInstance)]
            public void InstanceDependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.NonThreadAffinedInstance)]
            public void InstanceDependentMethod2()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.NonThreadAffinedInstance)]
            public void Exception()
            {
                throw new NotSupportedException();
            }

            [SingleThreaded(SingleThreadPolicy.ClassLevel)]
            public static void StaticException()
            {
                throw new NotSupportedException();
            }
        }

        public class SingleThreadedMethodsDerivedObject : SingleThreadedMethodsObject
        {
            [SingleThreaded(SingleThreadPolicy.MethodLevel)]
            public static void DerivedStaticIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.ClassLevel)]
            public static void DerivedStaticTypeDependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.ClassLevel)]
            public static void DerivedStaticTypeDependentMethod2()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.MethodLevel)]
            public void DerivedInstanceIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.MethodLevel)]
            public void DerivedInstanceIndependentMethod2()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.NonThreadAffinedInstance)]
            public void DerivedInstanceDependentMethod()
            {
                Thread.Sleep(200);
            }

            [SingleThreaded(SingleThreadPolicy.NonThreadAffinedInstance)]
            public void DerivedInstanceDependentMethod2()
            {
                Thread.Sleep(200);
            }
        }

        [SingleThreadedClass(SingleThreadedClassPolicy.ThreadAffined)]
        public class AffinedSingleThreadedObject
        {
            public void Sleep1()
            {
                Thread.Sleep(200);
            }

            public void Sleep2()
            {
                Thread.Sleep(200);
            }

            public void DoNothing1()
            {
            }

            public void DoNothing2()
            {
            }
        }
    }
}