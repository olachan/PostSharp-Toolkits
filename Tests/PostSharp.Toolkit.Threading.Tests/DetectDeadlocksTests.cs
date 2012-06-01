#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NUnit.Framework;
using PostSharp.Toolkit.Threading.DeadlockDetection;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class DetectDeadlocksTests
    {
        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void SimpleLock_WhenDeadlocked_Throws()
        {
            object lock1 = new object();
            object lock2 = new object();
            Barrier barrier = new Barrier(2);
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

            Action t2 = () =>
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

            TestHelpers.InvokeSimultaneouslyAndWaitForDeadlockDetection(t1, t2);
        }

        [Test]
        public void SimpleLock_WhenNoDeadlocked_DoesNotThrow()
        {
            object lock1 = new object();
            object lock2 = new object();

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

            TestHelpers.InvokeSimultaneouslyAndWaitForDeadlockDetection(t1, t2);
        }

        [Test]
        public void Mutex_WhenHandleManipulatedAndDeadlocked_DoesNotThrow()
        {
            Barrier barrier = new Barrier(3);
            Mutex mutex1 = new Mutex();
            Mutex mutex2 = new Mutex();

            Action t1 = () =>
                            {
                                mutex1.WaitOne();
                                barrier.SignalAndWait();
                                mutex2.WaitOne();
                                Thread.Sleep(100);
                                mutex2.ReleaseMutex();
                                mutex1.ReleaseMutex();
                            };

            Action t2 = () =>
                            {
                                mutex2.WaitOne();
                                barrier.SignalAndWait();
                                mutex1.WaitOne();
                                Thread.Sleep(100);
                                mutex1.ReleaseMutex();
                                mutex2.ReleaseMutex();
                            };


            Task.Factory.StartNew(
                () =>
                {
                    barrier.SignalAndWait();
                    SafeWaitHandle handle = mutex1.SafeWaitHandle;
                });

            TestHelpers.InvokeSimultaneouslyAndWaitForDeadlockDetection(t1, t2, 500);
        }

        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void Mutex_WhenDeadlocked_Throws()
        {
            Barrier barrier = new Barrier(2);
            Mutex mutex1 = new Mutex();
            Mutex mutex2 = new Mutex();
            Action t1 = () =>
                            {
                                mutex1.WaitOne();
                                barrier.SignalAndWait();
                                mutex2.WaitOne();
                                Thread.Sleep(100);
                                mutex2.ReleaseMutex();
                                mutex1.ReleaseMutex();
                            };

            Action t2 = () =>
                            {
                                mutex2.WaitOne();
                                barrier.SignalAndWait();
                                mutex1.WaitOne();
                                Thread.Sleep(100);
                                mutex1.ReleaseMutex();
                                mutex2.ReleaseMutex();
                            };

            TestHelpers.InvokeSimultaneouslyAndWaitForDeadlockDetection(t1, t2);
        }

        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void ReaderWriterSlim_WhenDeadlocked_Throws()
        {
            ReaderWriterSlimClass rw = new ReaderWriterSlimClass();
            Barrier barrier = new Barrier(2);
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

            TestHelpers.InvokeSimultaneouslyAndWaitForDeadlockDetection(t1, t2);
        }

        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void ReaderWriter_WhenDeadlocked_Throws()
        {
            ReaderWriterClass rw = new ReaderWriterClass();
            Barrier barrier = new Barrier(2);
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

            TestHelpers.InvokeSimultaneouslyAndWaitForDeadlockDetection(t1, t2);
        }

        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void ReaderWriterAttribute_WhenDeadlocked_Throws()
        {
            ReaderWriterAttributeClass rw = new ReaderWriterAttributeClass();
            Barrier barrier = new Barrier(2);
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

            TestHelpers.InvokeSimultaneouslyAndWaitForDeadlockDetection(t1, t2);
        }

        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void ReaderWriterAttribute_WhenInternal_Throws()
        {
            ReaderWriterWithObserverMethodClass rw = new ReaderWriterWithObserverMethodClass();
            Barrier barrier = new Barrier(2);
            rw.Write(100, () =>
            {
                Task t = new Task(
                    () =>
                    {
                        lock (rw)
                        {
                            barrier.SignalAndWait();
                            rw.Write(1, () => { });
                        }
                    });
                t.Start();
                barrier.SignalAndWait();
                lock (rw)
                {

                }
            });

            //rw.Read( 100 );
        }

        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void ReaderWriterWithObserverEvent_WhenDeadlocked_Throws()
        {
            ReaderWriterWithObserverEventClass rw = new ReaderWriterWithObserverEventClass();
            Barrier barrier = new Barrier(2);

            rw.OnWrite += () =>
            {
                Task t = new Task(
                    () =>
                    {
                        lock (rw)
                        {
                            barrier.SignalAndWait();
                            rw.Write(1);
                        }
                    });
                t.Start();
                barrier.SignalAndWait();
                lock (rw)
                {

                }
            };

            rw.Write(100);

            //rw.Read( 100 );
        }
       

        //        [Test]
        //        [ExpectedException(typeof(DeadlockException))]
        //        public void SynchronizedAttribute_WhenDeadlocked_Throws()
        //        {
        //            var rw = new SynchronizedAttributeClass();
        //            var barrier = new Barrier(2);
        //            int i = 0;
        //
        //            Action t1 = () => rw.Read(() =>
        //            {
        //                barrier.SignalAndWait();
        //                lock (rw)
        //                {
        //                    Debug.Print("lock acquired by thread {0}", Thread.CurrentThread.ManagedThreadId);
        //                    i = 1;
        //                }
        //            });
        //
        //            Action t2 = () =>
        //            {
        //                lock (rw)
        //                {
        //                    barrier.SignalAndWait();
        //                    rw.Write(i, () => { });
        //                }
        //                Debug.Print("lock released by thread {0}", Thread.CurrentThread.ManagedThreadId);
        //            };
        //
        //            TestHelpers.InvokeSimultaneouslyAndWaitForDeadlockDetection(t1, t2);
        //        }
    }

    //    public class SynchronizedAttributeClass
    //    {
    //        private int field;
    //
    //        [Synchronized]
    //        public int Read(Action action)
    //        {
    //            action();
    //            var value = this.field;
    //            return value;
    //        }
    //
    //        [Synchronized]
    //        public void Write(int value, Action action)
    //        {
    //            action();
    //            this.field = value;
    //        }
    //    }

    [ReaderWriterSynchronized]
    public class ReaderWriterAttributeClass
    {
        private int field;

        [ReaderLock]
        public int Read(Action action)
        {
            action();
            int value = this.field;
            return value;
        }

        [WriterLock]
        public void Write(int value, Action action)
        {
            action();
            this.field = value;
        }
    }

    


    public class ReaderWriterSlimClass
    {
        private ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();

        private int field;

        public int Read(Action action)
        {
            rwl.EnterReadLock();
            action();
            int value = field;
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

    public class ReaderWriterClass
    {
        private ReaderWriterLock rwl = new ReaderWriterLock();

        private int field;

        public int Read(Action action)
        {
            rwl.AcquireReaderLock(Timeout.Infinite);
            action();
            int value = field;
            rwl.ReleaseReaderLock();
            return value;
        }

        public void Write(int value, Action action)
        {
            rwl.AcquireWriterLock(Timeout.Infinite);
            action();
            this.field = value;
            rwl.ReleaseWriterLock();
        }
    }
}