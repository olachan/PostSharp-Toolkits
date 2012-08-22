#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;
using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class ThreadUnsafeTests
    {
        [TearDown]
        public void TearDown()
        {
            // wait for any pending exceptions from background tasks
            try
            {
                GC.Collect(GC.MaxGeneration);
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }

#if !(DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void BuildConfigurationTest()
        {
            Assert.Inconclusive("ThreadUnsafeTests can run only in DEBUG configuration");
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void MethodsInvokedOnSeparateObjects_NoException()
        {
            SingleThreadedMethodsObject o1 = new SingleThreadedMethodsObject();
            SingleThreadedMethodsObject o2 = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o1.InstanceDependentMethod, o2.InstanceDependentMethod );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
#endif
        public void InstanceDependentDerivedAndNotDerivedMethodInvoked_Exception()
        {
            SingleThreadedMethodsDerivedObject o1 = new SingleThreadedMethodsDerivedObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o1.DerivedInstanceDependentMethod, o1.InstanceDependentMethod );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
#endif
        public void SameInstanceDependentMethodInvokedTwice_Exception()
        {
            SingleThreadedMethodsObject o1 = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o1.InstanceDependentMethod, o1.InstanceDependentMethod );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
#endif
        public void TwoInstanceDependentMethodsInvoked_Exception()
        {
            SingleThreadedMethodsObject o1 = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o1.InstanceDependentMethod, o1.InstanceDependentMethod2 );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
#endif
        public void TypeDependentStaticMethodInvokedTwice_Exception()
        {
            TestHelpers.InvokeSimultaneouslyAndWait( SingleThreadedStaticMethodsObject.StaticTypeDependentMethod,
                                                     SingleThreadedStaticMethodsObject.StaticTypeDependentMethod );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
#endif
        public void TwoTypeDependentStaticMethodInvoked_Exception()
        {
            TestHelpers.InvokeSimultaneouslyAndWait( SingleThreadedStaticMethodsObject.StaticTypeDependentMethod,
                                                     SingleThreadedStaticMethodsObject.StaticTypeDependentMethod2 );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
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

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
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
                                                             TestHelpers.Swallow<NotSupportedException>(
                                                                 SingleThreadedStaticMethodsObject.StaticTypeDependentMethod );
                                                         } );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void SingleThreadedClassGetter_MultipleAccessesDoNotThrow()
        {
            SingleThreadedClassObject o = new SingleThreadedClassObject();
            int x;
            TestHelpers.InvokeSimultaneouslyAndWait( () => { x = o.TestProperty; }, () => { x = o.TestProperty; } );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(ThreadUnsafeException) )]
#endif
        public void SingleThreadedClassSetter_MultipleAccessesThrow()
        {
            SingleThreadedClassObject o = new SingleThreadedClassObject();
            TestHelpers.InvokeSimultaneouslyAndWait( () => { o.TestProperty = 3; },
                                                     () => { o.TestProperty = 3; } );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void ThreadSafeMethod_InvokedFromTwoThreads_DoesNotThrow()
        {
            SingleThreadedMethodsObject o = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o.ThreadSafeMethod, o.ThreadSafeMethod );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void PrivateThreadUnsafeMethod_InvokedFromThreadSafeInTwoThreads_DoesNotThrow()
        {
            SingleThreadedMethodsObject o = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o.ThreadSafeMethodInvokingThreadUnsafePrivateMethod, o.ThreadSafeMethodInvokingThreadUnsafePrivateMethod );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void PrivateThreadSafeMethod_InvokedFromTwoThreads_DoesNotThrow()
        {
            SingleThreadedMethodsObject o = new SingleThreadedMethodsObject();
            TestHelpers.InvokeSimultaneouslyAndWait( o.ThreadSafeMethodInvokingThreadSafePrivateMethod, o.ThreadSafeMethodInvokingThreadSafePrivateMethod );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void NonThreadSafeField_ModifiedFromThreadSafeMethod_DoesNotThrow()
        {
            ThreadUnsafeWithFieldAccessCheckObject o = new ThreadUnsafeWithFieldAccessCheckObject();
            o.ChangeNonThreadSafeFieldFromThreadSafe();
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void ThreadSafeField_ModifiedFromThreadSafeMethod_DoesNotThrow()
        {
            ThreadUnsafeWithFieldAccessCheckObject o = new ThreadUnsafeWithFieldAccessCheckObject();
            o.ChangeThreadSafeFieldFromThreadSafe();
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(LockNotHeldException) )]
#endif
        public void NonThreadSafeField_ModifiedFromStaticUnsafeMethod_Throws()
        {
            ThreadUnsafeWithFieldAccessCheckObject o = new ThreadUnsafeWithFieldAccessCheckObject();
            ThreadUnsafeWithFieldAccessCheckObject.ChangeNonThreadSafeField( o );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(LockNotHeldException) )]
#endif
        public void NonThreadSafeField_ModifiedFromStaticSafeMethod_Throws()
        {
            ThreadUnsafeWithFieldAccessCheckObject o = new ThreadUnsafeWithFieldAccessCheckObject();
            ThreadUnsafeWithFieldAccessCheckObject.ChangeNonThreadSafeFieldFromThreadSafe( o );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void ThreadSafeField_ModifiedFromStaticMethod_DoesNotThrow()
        {
            ThreadUnsafeWithFieldAccessCheckObject o = new ThreadUnsafeWithFieldAccessCheckObject();
            ThreadUnsafeWithFieldAccessCheckObject.ChangeThreadSafeField( o );
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void ThreadSafeStaticField_Modified_NeverThrows()
        {
            ThreadUnsafeWithFieldAccessCheckStaticClass o = new ThreadUnsafeWithFieldAccessCheckStaticClass();
            o.ChangeThreadSafeStaticField();
            ThreadUnsafeWithFieldAccessCheckStaticClass.ChangeThreadSafeStaticFieldStatic();
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void NonThreadSafeStaticField_ModifiedByStaticMethod_DoesNotThrow()
        {
            ThreadUnsafeWithFieldAccessCheckStaticClass.ChangeNonThreadSafeStaticFieldStatic();
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException( typeof(LockNotHeldException) )]
#endif
        public void NonThreadSafeStaticField_ModifiedExternaly_Throws()
        {
            ThreadUnsafeWithFieldAccessCheckStaticClass.NonThreadSafeStaticField++;
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void ThreadSafeStaticField_ModifiedExternaly_DoesNotThrow()
        {
            ThreadUnsafeWithFieldAccessCheckStaticClass.ThreadSafeStaticField++;
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void ThreadSafeProperty_ModifiedExternaly_DoesNotThrow()
        {
            new ThreadUnsafeWithFieldAccessCheckObject().ThreadSafeProperty = 7;
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void NonThreadSafeProperty_ModifiedExternaly_DoesNotThrows()
        {
            new ThreadUnsafeWithFieldAccessCheckObject().NonThreadSafeProperty = 7;
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
                    return this._testProperty;
                }
                set
                {
                    Thread.Sleep( 200 );
                    this._testProperty = value;
                }
            }
        }

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
                Thread.Sleep( 200 );
            }

            [ThreadUnsafeMethod]
            private void ThreadUnsafePrivateMethod()
            {
                Thread.Sleep( 200 );
            }

            private void ThreadSafePrivateMethod()
            {
                Thread.Sleep( 200 );
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


        // [ThreadUnsafeObject( ThreadUnsafePolicy.Static )]
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

        // [ThreadUnsafeObject]
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

        [ThreadUnsafeObject( CheckFieldAccess = true )]
        public class ThreadUnsafeWithFieldAccessCheckObject
        {
            [ThreadSafe] private int threadSafeField = 11;

            private int nonThreadSafeField = 12;

            public int ThreadSafeProperty { [ThreadSafe]
            get; [ThreadSafe]
            set; }

            public int NonThreadSafeProperty { get; set; }

            [ThreadSafe]
            public void ChangeThreadSafeFieldFromThreadSafe()
            {
                this.threadSafeField++;
            }

            [ThreadSafe]
            public void ChangeNonThreadSafeFieldFromThreadSafe()
            {
                this.nonThreadSafeField++;
            }

            public static void ChangeThreadSafeField( ThreadUnsafeWithFieldAccessCheckObject obj )
            {
                ++obj.threadSafeField;
            }

            public static void ChangeNonThreadSafeField( ThreadUnsafeWithFieldAccessCheckObject obj )
            {
                ++obj.nonThreadSafeField;
            }

            public static void ChangeNonThreadSafeFieldFromThreadSafe( ThreadUnsafeWithFieldAccessCheckObject obj )
            {
                ++obj.nonThreadSafeField;
            }
        }

        [ThreadUnsafeObject( ThreadUnsafePolicy.Static, CheckFieldAccess = true )]
        public class ThreadUnsafeWithFieldAccessCheckStaticClass
        {
            public static int NonThreadSafeStaticField = 21;

            [ThreadSafe] public static int ThreadSafeStaticField = 22;

            public void ChangeThreadSafeStaticField()
            {
                ThreadSafeStaticField++;
            }

            public void ChangeNonThreadSafeStaticField()
            {
                NonThreadSafeStaticField++;
            }

            public static void ChangeThreadSafeStaticFieldStatic()
            {
                ThreadSafeStaticField++;
            }

            public static void ChangeNonThreadSafeStaticFieldStatic()
            {
                NonThreadSafeStaticField++;
            }
        }
    }
}