using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using PostSharp.Toolkit.Threading.SingleThreaded;
using PostSharp.Toolkit.Threading.Synchronized;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class SynchronizedTests
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
        public void TwoInstanceIndependentMethodsInvoked_NoWait()
        {
            var time = this.InvokeAndTraceTime(() =>
               {
                   var o1 = new SynchronizedEntity();
                   InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod2);
               });

            Assert.Less(time, 300);
        }

        [Test]
        public void InstanceDependentAndIndependentMethodsInvoked_NoWait()
        {
            var time = this.InvokeAndTraceTime(() =>
               {
                   var o1 = new SynchronizedEntity();
                   InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceDependentMethod);
               });

            Assert.Less(time, 300);
        }

        [Test]
        public void MethodsInvokedOnSeparateObjects_NoWait()
        {
            var time = this.InvokeAndTraceTime(() =>
               {
                   var o1 = new SynchronizedEntity();
                   var o2 = new SynchronizedEntity();
                   InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o2.InstanceDependentMethod);
               });

            Assert.Less(time, 300);
        }

        [Test]
        public void SameInstanceIndependentMethodInvokedTwice_Waits()
        {
            var time = this.InvokeAndTraceTime(() =>
               {
                   var o1 = new SynchronizedEntity();
                   InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.InstanceIndependentMethod);
               });

            Assert.GreaterOrEqual(time, 400);
        }

        [Test]
        public void SameInstanceDependentMethodInvokedTwice_Waits()
        {
            var time = this.InvokeAndTraceTime(() =>
                {
                    var o1 = new SynchronizedEntity();
                    InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o1.InstanceDependentMethod);
                });

            Assert.GreaterOrEqual(time, 400);
        }

        [Test]
        public void TwoInstanceDependentMethodsInvoked_Waits()
        {
            var time = this.InvokeAndTraceTime(() =>
               {
                   var o1 = new SynchronizedEntity();
                   InvokeSimultaneouslyAndWait(o1.InstanceDependentMethod, o1.InstanceDependentMethod2);
               });

            Assert.GreaterOrEqual(time, 400);
        }

        [Test]
        public void TypeIndependentAndDependentStaticMethodsInvoked_NoWait()
        {
            var time = this.InvokeAndTraceTime(() => this.InvokeSimultaneouslyAndWait(SynchronizedEntity.StaticIndependentMethod, SynchronizedEntity.StaticTypeDependentMethod));

            Assert.Less(time, 600);
        }

        [Test]
        public void TypeIndependentStaticMethodInvokedTwice_Waits()
        {
            var time = this.InvokeAndTraceTime(() => this.InvokeSimultaneouslyAndWait(SynchronizedEntity.StaticIndependentMethod, SynchronizedEntity.StaticIndependentMethod));

            Assert.GreaterOrEqual(time, 400);
        }

        [Test]
        public void TypeDependentStaticMethodInvokedTwice_Waits()
        {
            var time = this.InvokeAndTraceTime(() => this.InvokeSimultaneouslyAndWait(SynchronizedEntity.StaticTypeDependentMethod, SynchronizedEntity.StaticTypeDependentMethod));

            Assert.GreaterOrEqual(time, 400);
        }

        [Test]
        public void TwoTypeDependentStaticMethodInvoked_Waits()
        {
            var time = this.InvokeAndTraceTime(() => this.InvokeSimultaneouslyAndWait(SynchronizedEntity.StaticTypeDependentMethod, SynchronizedEntity.StaticTypeDependentMethod2));

            Assert.GreaterOrEqual(time, 400);
        }

        [Test]
        public void TwoInstanceIndependentDerivedMethodsInvoked_NoWait()
        {
            var o1 = new SynchronizedMethodsDerivedObject();
            var time = this.InvokeAndTraceTime(() => InvokeSimultaneouslyAndWait(o1.DerivedInstanceIndependentMethod, o1.DerivedInstanceIndependentMethod2));

            Assert.Less(time, 300);
        }

        [Test]
        public void InstanceIndependentDerivedAndOntDerivedMethodsInvoked_NoWait()
        {
            var o1 = new SynchronizedMethodsDerivedObject();
            var time = this.InvokeAndTraceTime(() => InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.DerivedInstanceIndependentMethod2));

            Assert.Less(time, 300);
        }

        [Test]
        public void InstanceDependentAndIndependentDerivedMethodsInvoked_NoWait()
        {
            var o1 = new SynchronizedMethodsDerivedObject();
            var time = this.InvokeAndTraceTime(() => InvokeSimultaneouslyAndWait(o1.InstanceIndependentMethod, o1.DerivedInstanceDependentMethod));

            Assert.Less(time, 300);
        }

        [Test]
        public void InstanceIndependentDerivedAndNotDerivedMethodInvoked_NoWait()
        {
            var o1 = new SynchronizedMethodsDerivedObject();
            var time = this.InvokeAndTraceTime(() => InvokeSimultaneouslyAndWait(o1.DerivedInstanceIndependentMethod, o1.InstanceIndependentMethod));

            Assert.Less(time, 300);
        }

        [Test]
        public void InstanceDependentDerivedAndNotDerivedMethodInvoked_Waits()
        {
            var o1 = new SynchronizedMethodsDerivedObject();
            var time = this.InvokeAndTraceTime(() => InvokeSimultaneouslyAndWait(o1.DerivedInstanceDependentMethod, o1.InstanceDependentMethod));

            Assert.GreaterOrEqual(time, 400);
        }

        [Test]
        public void MethodThrowsException_MonitorProperlyReleased()
        {
            var o1 = new SynchronizedEntity();
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(o1.Exception), () => { Thread.Sleep(500); Swallow<NotSupportedException>(o1.Exception); });
            InvokeSimultaneouslyAndWait(() => Swallow<NotSupportedException>(o1.Exception), () => { Thread.Sleep(500); Swallow<NotSupportedException>(o1.InstanceDependentMethod); });
        }

        [Test]
        public void StaticMethodThrowsException_MonitorProperlyReleased()
        {
            InvokeSimultaneouslyAndWait(
                                         () => Swallow<NotSupportedException>(SynchronizedEntity.StaticException),
                                         () => { Thread.Sleep(500); Swallow<NotSupportedException>(SynchronizedEntity.StaticException); });
            InvokeSimultaneouslyAndWait(
                                         () => Swallow<NotSupportedException>(SynchronizedEntity.StaticException),
                                         () => { Thread.Sleep(500); Swallow<NotSupportedException>(SynchronizedEntity.StaticTypeDependentMethod); });
        }

        [Test]
        public void SynchronizedClassGetter_MultipleAccessesDoNotWait()
        {
            var o = new SynchronizedClassObject();
            int x;
            var time = this.InvokeAndTraceTime(() => { InvokeSimultaneouslyAndWait(() => { x = o.TestProperty; }, () => { x = o.TestProperty; }); });

            Assert.Less(time, 300);
        }


        [Test]
        public void SynchronizedClassSetter_MultipleAccessesWaits()
        {
            var o = new SynchronizedClassObject();
            var time = this.InvokeAndTraceTime(
                () =>
                { InvokeSimultaneouslyAndWait(() => { o.TestProperty = 3; }, () => { o.TestProperty = 3; }); });

            Assert.GreaterOrEqual(time, 400);

        }

        [Test]
        public void SynchronizedClassWithNotIgnoredGetters_MultipleGetterAccessesWaits()
        {
            var o = new SynchronizedClassNotIgnoreGettersObject();
            var time =
                this.InvokeAndTraceTime(
                    () => { InvokeSimultaneouslyAndWait(() => { o.TestProperty = 3; }, () => { o.TestProperty = 3; }); });

            Assert.GreaterOrEqual(time, 400);
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

        [SynchronizedClass]
        public class SynchronizedClassObject
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

        [SynchronizedClass(IgnoreGetters = false)]
        public class SynchronizedClassNotIgnoreGettersObject
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

        public class SynchronizedMethodsDerivedObject : SynchronizedEntity
        {
            [Synchronized(false)]
            public static void DerivedStaticIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [Synchronized(true)]
            public static void DerivedStaticTypeDependentMethod()
            {
                Thread.Sleep(200);
            }

            [Synchronized(true)]
            public static void DerivedStaticTypeDependentMethod2()
            {
                Thread.Sleep(200);
            }

            [Synchronized(false)]
            public void DerivedInstanceIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [Synchronized(false)]
            public void DerivedInstanceIndependentMethod2()
            {
                Thread.Sleep(200);
            }

            [Synchronized(true)]
            public void DerivedInstanceDependentMethod()
            {
                Thread.Sleep(200);
            }

            [Synchronized(true)]
            public void DerivedInstanceDependentMethod2()
            {
                Thread.Sleep(200);
            }
        }

        public class SynchronizedEntity
        {
            [Synchronized(false)]
            public static void StaticIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [Synchronized(true)]
            public static void StaticTypeDependentMethod()
            {
                Thread.Sleep(200);
            }

            [Synchronized(true)]
            public static void StaticTypeDependentMethod2()
            {
                Thread.Sleep(200);
            }

            [Synchronized(false)]
            public void InstanceIndependentMethod()
            {
                Thread.Sleep(200);
            }

            [Synchronized(false)]
            public void InstanceIndependentMethod2()
            {
                Thread.Sleep(200);
            }

            [Synchronized(true)]
            public void InstanceDependentMethod()
            {
                Thread.Sleep(200);
            }

            [Synchronized(true)]
            public void InstanceDependentMethod2()
            {
                Thread.Sleep(200);
            }

            [Synchronized]
            public void Exception()
            {
                throw new NotSupportedException();
            }

            [Synchronized]
            public static void StaticException()
            {
                throw new NotSupportedException();
            }
        }
    }
}
