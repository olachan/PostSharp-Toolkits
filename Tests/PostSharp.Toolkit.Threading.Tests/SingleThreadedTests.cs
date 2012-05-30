#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class SingleThreadedTests
    {
        [Test]
        public void MethodsInvokedOnSeparateObjects_NoException()
        {
            SingleThreadedMethodsObject o1 = new SingleThreadedMethodsObject();
            SingleThreadedMethodsObject o2 = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o1.InstanceDependentMethod, o2.InstanceDependentMethod );
        }

        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
        public void InstanceDependentDerivedAndNotDerivedMethodInvoked_Exception()
        {
            SingleThreadedMethodsDerivedObject o1 = new SingleThreadedMethodsDerivedObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o1.DerivedInstanceDependentMethod, o1.InstanceDependentMethod );
        }

        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
        public void SameInstanceDependentMethodInvokedTwice_Exception()
        {
            SingleThreadedMethodsObject o1 = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o1.InstanceDependentMethod, o1.InstanceDependentMethod );
        }

        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
        public void TwoInstanceDependentMethodsInvoked_Exception()
        {
            SingleThreadedMethodsObject o1 = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o1.InstanceDependentMethod, o1.InstanceDependentMethod2 );
        }

        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
        public void TypeDependentStaticMethodInvokedTwice_Exception()
        {
            TestHelpers.InvokeSimultaneouslyAndWait( SingleThreadedStaticMethodsObject.StaticTypeDependentMethod,
                                         SingleThreadedStaticMethodsObject.StaticTypeDependentMethod );
        }

        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
        public void TwoTypeDependentStaticMethodInvoked_Exception()
        {
            TestHelpers.InvokeSimultaneouslyAndWait( SingleThreadedStaticMethodsObject.StaticTypeDependentMethod,
                                         SingleThreadedStaticMethodsObject.StaticTypeDependentMethod2 );
        }

        [Test]
        public void MethodThrowsException_MonitorProperlyReleased()
        {
            SingleThreadedMethodsObject o1 = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( () => TestHelpers.Swallow<NotSupportedException>( o1.Exception ),
                                         () =>
                                             {
                                                 Thread.Sleep( 200 );
                                                 TestHelpers.Swallow<NotSupportedException>( o1.Exception );
                                             } );
            TestHelpers.InvokeSimultaneouslyAndWait( () => TestHelpers.Swallow<NotSupportedException>( o1.Exception ),
                                         () =>
                                             {
                                                 Thread.Sleep( 200 );
                                                 TestHelpers.Swallow<NotSupportedException>( o1.InstanceDependentMethod );
                                             } );
        }

        [Test]
        public void StaticMethodThrowsException_MonitorProperlyReleased()
        {
            TestHelpers.InvokeSimultaneouslyAndWait( () => TestHelpers.Swallow<NotSupportedException>( SingleThreadedMethodsObject.StaticException ),
                                         () =>
                                             {
                                                 Thread.Sleep( 200 );
                                                 TestHelpers.Swallow<NotSupportedException>( SingleThreadedMethodsObject.StaticException );
                                             } );
            TestHelpers.InvokeSimultaneouslyAndWait( () => TestHelpers.Swallow<NotSupportedException>( SingleThreadedMethodsObject.StaticException ),
                                         () =>
                                             {
                                                 Thread.Sleep( 200 );
                                                 TestHelpers.Swallow<NotSupportedException>( SingleThreadedStaticMethodsObject.StaticTypeDependentMethod );
                                             } );
        }

        [Test]
        public void SingleThreadedClassGetter_MultipleAccessesDoNotThrow()
        {
            var o = new SingleThreadedClassObject();
            int x;
            TestHelpers.InvokeSimultaneouslyAndWait(() => { x = o.TestProperty; }, () => { x = o.TestProperty; });
        }

        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
        public void SingleThreadedClassSetter_MultipleAccessesThrow()
        {
            SingleThreadedClassObject o = new SingleThreadedClassObject();
            TestHelpers.InvokeSimultaneouslyAndWait( () => { o.TestProperty = 3; },
                                         () => { o.TestProperty = 3; } );
        }

        [Test]
        public void ThreadSafeMethod_InvokedFromTwoThreads_DoesNotThrow()
        {
            SingleThreadedMethodsObject o = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait(o.ThreadSafeMethod, o.ThreadSafeMethod);
        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void PrivateThreadUnsafeMethod_InvokedFromTwoThreads_DoesThrow()
        {
            SingleThreadedMethodsObject o = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait(o.ThreadSafeMethodInvokingThreadUnsafePrivateMethod, o.ThreadSafeMethodInvokingThreadUnsafePrivateMethod);
        }

        [Test]
        public void PrivateThreadSafeMethod_InvokedFromTwoThreads_DoesNotThrow()
        {
            SingleThreadedMethodsObject o = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait(o.ThreadSafeMethodInvokingThreadSafePrivateMethod, o.ThreadSafeMethodInvokingThreadSafePrivateMethod);
        }

        [ThreadUnsafeObject]
        public class SingleThreadedClassObject
        {
            private int _testProperty;

            public int TestProperty
            {
                [ThreadSafe]
                get
                {
                    Thread.Sleep( 200 );
                    return _testProperty;
                }
                set
                {
                    Thread.Sleep( 200 );
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

        [ThreadUnsafeObject( ThreadUnsafePolicy.Static )]
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

        [ThreadUnsafeObject]
        public class SingleThreadedMethodsObject
        {
            [ThreadSafe]
            public void ThreadSafeMethod()
            {
                Thread.Sleep(200);
            }

            [ThreadUnsafeMethod]
            private void ThreadUnsafePrivateMethod()
            {
                Thread.Sleep( 200 );
            }

            private void ThreadSafePrivateMethod()
            {
                Thread.Sleep(200);
            }

            [ThreadSafe]
            public void ThreadSafeMethodInvokingThreadUnsafePrivateMethod()
            {
                this.ThreadUnsafePrivateMethod();
            }

            [ThreadSafe]
            public void ThreadSafeMethodInvokingThreadSafePrivateMethod()
            {
                this.ThreadSafePrivateMethod();
            }

            public void InstanceDependentMethod()
            {
                Thread.Sleep( 200 );
            }

            public void InstanceDependentMethod2()
            {
                Thread.Sleep( 200 );
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


        [ThreadUnsafeObject( ThreadUnsafePolicy.Static )]
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

        [ThreadUnsafeObject]
        public class SingleThreadedMethodsDerivedObject : SingleThreadedMethodsObject
        {
            public void DerivedInstanceDependentMethod()
            {
                Thread.Sleep( 200 );
            }

            public void DerivedInstanceDependentMethod2()
            {
                Thread.Sleep( 200 );
            }
        }
    }
}