using System;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using PostSharp.Toolkit.Threading.Deadlock;

[assembly: DetectDeadlocks(AttributeTargetAssemblies = "mscorlib", AttributeTargetTypes = "System.Threading.*")]
[assembly: DetectDeadlocks(AttributeTargetAssemblies = "System.Core", AttributeTargetTypes = "System.Threading.*")]

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class DetectDeadlocksTests
    {
        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void SimpleLock_WhenDeadlocked_Throws()
        {
            var lock1 = new object();
            var lock2 = new object();
            var barrier = new Barrier(2);
            Action t1 = () =>
                {
                    lock (lock1)
                    {
                        barrier.SignalAndWait();
                        lock (lock2)
                        {
                            Thread.Sleep(100);
                        }
                    }
                };

            Action t2 =() =>
            {
                lock (lock2)
                {
                    barrier.SignalAndWait();
                    lock (lock1)
                    {
                        Thread.Sleep(100);
                    }
                }
            };

            TestHelpers.InvokeSimultaneouslyAndWait(t1, t2);
        }

        [Test]
        public void SimpleLock_WhenNoDeadlocked_DoesNotThrows()
        {
            var lock1 = new object();
            var lock2 = new object();
            
            Action t1 = () =>
            {
                lock (lock1)
                {
                    Thread.Sleep(500);
                    lock (lock2)
                    {
                        Thread.Sleep(500);
                    }
                }
            };

            Action t2 = () =>
            {
                lock (lock1)
                {
                    Thread.Sleep(500);
                    lock (lock2)
                    {
                        Thread.Sleep(500);
                    }
                }
            };

            TestHelpers.InvokeSimultaneouslyAndWait(t1, t2);
        }

        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void ReaderWriter_WhenDeadlocked_Throws()
        {
            var rw = new ReaderWriterClass();
            var barrier = new Barrier(2);
            int i = 0;

            Action t1 = () => rw.Read(() =>
                {
                    barrier.SignalAndWait();
                    lock (rw)
                    {
                        i = 1;
                    }
                });

            Action t2 = () =>
            {
                lock (rw)
                {
                    barrier.SignalAndWait();
                    rw.Write(i, () => { });
                }
            };

            TestHelpers.InvokeSimultaneouslyAndWait(t1, t2);
        }
    }

    public class ReaderWriterClass
    {
        ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();

        private int field;

        public int Read(Action action)
        {
            rwl.EnterReadLock();
            action();
            var value = field;
            rwl.ExitReadLock();
            return value;
        }

        public void Write(int value, Action action)
        {
            rwl.EnterWriteLock();
            action();
            this.field = value;
            rwl.ExitWriteLock();
        }
    }
}
