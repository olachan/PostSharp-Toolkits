using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PostSharp.Toolkit.Threading.Dispatching;

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
//
//        [Test]
//        public void TwoInstanceIndependentMethodsInvoked_NoException()
//        {
//            var o1 = new SingleThreadedMethodsObject();
//            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod2);
//        }
//
//        [Test]
//        public void TwoInstanceIndependentDerivedMethodsInvoked_NoException()
//        {
//            var o1 = new SingleThreadedMethodsDerivedObject();
//            InvokeSimultaneouslyAndWait(o1.DerivedInstanceIndependentMethod, o1.DerivedInstanceIndependentMethod2);
//        }
//
//        [Test]
//        public void InstanceIndependentDerivedAndOntDerivedMethodsInvoked_NoException()
//        {
//            var o1 = new SingleThreadedMethodsDerivedObject();
//            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.DerivedInstanceIndependentMethod2);
//        }
//
//        [Test]
//        public void InstanceDependentAndIndependentMethodsInvoked_NoException()
//        {
//            var o1 = new SingleThreadedMethodsObject();
//            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceDependentMethod);
//        }
//
//        [Test]
//        public void InstanceDependentAndIndependentDerivedMethodsInvoked_NoException()
//        {
//            var o1 = new SingleThreadedMethodsDerivedObject();
//            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.DerivedInstanceDependentMethod);
//        }

        [Test]
        public void MethodsInvokedOnSeparateObjects_NoException()
        {
            var o1 = new SingleThreadedMethodsObject();
            var o2 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o2.InstanceDependentMethod);
        }

//        [Test]
//        [ExpectedException(typeof(ThreadUnsafeException))]
//        public void SameInstanceIndependentMethodInvokedTwice_Exception()
//        {
//            var o1 = new SingleThreadedMethodsObject();
//            InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod);
//        }
//
//        [Test]
//        public void InstanceIndependentDerivedAndNotDerivedMethodInvoked_NoException()
//        {
//            var o1 = new SingleThreadedMethodsDerivedObject();
//            InvokeSimultaneouslyAndWait(o1.DerivedInstanceIndependentMethod, o1.InstanceIndependentMethod);
//        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void InstanceDependentDerivedAndNotDerivedMethodInvoked_Exception()
        {
            var o1 = new SingleThreadedMethodsDerivedObject();
            InvokeSimultaneouslyAndWait(o1.DerivedInstanceDependentMethod, o1.InstanceDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void SameInstanceDependentMethodInvokedTwice_Exception()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o1.InstanceDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void TwoInstanceDependentMethodsInvoked_Exception()
        {
            var o1 = new SingleThreadedMethodsObject();
            InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o1.InstanceDependentMethod2);
        }

//        [Test]
//        public void TypeIndependentAndDependentStaticMethodsInvoked_NoException()
//        {
//            InvokeSimultaneouslyAndWait(SingleThreadedMethodsObject.StaticIndependentMethod,
//                                        SingleThreadedMethodsObject.StaticTypeDependentMethod);
//        }
//
//        [Test]
//        [ExpectedException(typeof(ThreadUnsafeException))]
//        public void TypeIndependentStaticMethodInvokedTwice_Exception()
//        {
//            InvokeSimultaneouslyAndWait(SingleThreadedMethodsObject.StaticIndependentMethod,
//                                        SingleThreadedMethodsObject.StaticIndependentMethod);
//        }
//
//        [Test]
//        public void TypeIndependentStaticDerivedMethodAndNotDerivedInvoked_NoException()
//        {
//            InvokeSimultaneouslyAndWait(SingleThreadedMethodsDerivedObject.DerivedStaticIndependentMethod,
//                                        SingleThreadedMethodsDerivedObject.StaticIndependentMethod);
//        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void TypeDependentStaticMethodInvokedTwice_Exception()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedStaticMethodsObject.StaticTypeDependentMethod,
                                        SingleThreadedStaticMethodsObject.StaticTypeDependentMethod);
        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void TwoTypeDependentStaticMethodInvoked_Exception()
        {
            InvokeSimultaneouslyAndWait(SingleThreadedStaticMethodsObject.StaticTypeDependentMethod,
                                        SingleThreadedStaticMethodsObject.StaticTypeDependentMethod2);
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
                                        () => { Thread.Sleep(200); Swallow<NotSupportedException>(SingleThreadedStaticMethodsObject.StaticTypeDependentMethod); });
        }

//        [Test]
//        public void SingleThreadedClassGetter_MultipleAccessesDoNotThrow()
//        {
//            var o = new SingleThreadedClassObject();
//            int x;
//            InvokeSimultaneouslyAndWait(() => { x = o.TestProperty; },
//                                        () => { x = o.TestProperty; });
//        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void SingleThreadedClassSetter_MultipleAccessesThrow()
        {
            var o = new SingleThreadedClassObject();
            InvokeSimultaneouslyAndWait(() => { o.TestProperty = 3; },
                                        () => { o.TestProperty = 3; });
        }

//        [Test]
//        public void SingleThreadedClassWithIgnoredSetters_MultipleSetterAccessesDoNotThrow()
//        {
//            var o = new SingleThreadedClassIngoreSettersObject();
//            InvokeSimultaneouslyAndWait(() => { o.TestProperty = 3; },
//                                        () => { o.TestProperty = 3; });
//        }

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


        [ThreadUnsafeClass]
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

//        [ThreadUnsafeClass(IgnoreSetters = true)]
//        public class SingleThreadedClassIngoreSettersObject
//        {
//            private int _testProperty;
//            public int TestProperty
//            {
//                get
//                {
//                    Thread.Sleep(200);
//                    return _testProperty;
//                }
//                set
//                {
//                    Thread.Sleep(200);
//                    _testProperty = value;
//                }
//            }
//        }

        [ThreadUnsafeClass( ThreadUnsafePolicy.Static )]
        public class SingleThreadedStaticMethodsObject
        {

            public static void StaticTypeDependentMethod()
            {
                Thread.Sleep( 200 );
            }

            public static void StaticTypeDependentMethod2()
            {
                Thread.Sleep( 200 );
            }
        }

        [ThreadUnsafeClass]
        public class SingleThreadedMethodsObject
        {
           
          

            public void InstanceDependentMethod()
            {
                Thread.Sleep(200);
            }

            public void InstanceDependentMethod2()
            {
                Thread.Sleep(200);
            }

            public void Exception()
            {
                throw new NotSupportedException();
            }

            public static void StaticException()
            {
                throw new NotSupportedException();
            }
        }


        [ThreadUnsafeClass( ThreadUnsafePolicy.Static )]
        public class SingleThreadedStaticMethodsDerivedObject : SingleThreadedStaticMethodsObject
        {

            public static void DerivedStaticTypeDependentMethod()
            {
                Thread.Sleep( 200 );
            }

            public static void DerivedStaticTypeDependentMethod2()
            {
                Thread.Sleep( 200 );
            }
        }

        [ThreadUnsafeClass]
        public class SingleThreadedMethodsDerivedObject : SingleThreadedMethodsObject
        {
            
          

            
            public void DerivedInstanceDependentMethod()
            {
                Thread.Sleep(200);
            }

            public void DerivedInstanceDependentMethod2()
            {
                Thread.Sleep(200);
            }
        }
    }
}