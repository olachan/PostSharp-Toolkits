﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class ReaderWriterSynchronizedTests : ThreadingBaseTestFixture
    {
        protected void InvokeSimultaneouslyAndWait( Action action1, Action action2 )
        {
            try
            {
                Task t1 = new Task( action1 );
                Task t2 = new Task( action2 );
                t1.Start();
                Thread.Sleep( 20 ); // Ensure deterministic order
                t2.Start();
                t1.Wait();
                t2.Wait();
            }
            catch ( AggregateException aggregateException )
            {
                Thread.Sleep( 500 ); //Make sure the second running task is over as well
                if ( aggregateException.InnerExceptions.Count == 1 )
                {
                    throw aggregateException.InnerException;
                }
                throw;
            }
        }

        [Test]
        public void TwoReadersCanRead()
        {
            ReaderWriterEntity rw = new ReaderWriterEntity();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            this.InvokeSimultaneouslyAndWait( () => rw.Read( 100 ), () => rw.Read( 100 ) );
            stopwatch.Stop();
            Assert.Less( stopwatch.ElapsedMilliseconds, 150 );
        }

        [Test]
        public void WhenWriterWritesReaderWaits()
        {
            ReaderWriterEntity rw = new ReaderWriterEntity();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            this.InvokeSimultaneouslyAndWait( () => rw.Write( 100, 101 ), () => rw.Read( 101 ) );
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 150);
        }

        [Test]
        public void WhenReaderReadsWriterWaits()
        {
            ReaderWriterEntity rw = new ReaderWriterEntity();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            this.InvokeSimultaneouslyAndWait( () => rw.Write( 100, 101 ), () => rw.Read( 101 ) );
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 150);
        }

        [Test]
        public void WhenWriterWritesWriterWaits()
        {
            ReaderWriterEntity rw = new ReaderWriterEntity();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            this.InvokeSimultaneouslyAndWait( () => rw.Write( 100, 101 ), () => rw.Write( 100, 101 ) );
            stopwatch.Stop();
            Assert.GreaterOrEqual( stopwatch.ElapsedMilliseconds, 150 );
        }

        [Test]
        public void TwoObjectsCanWriteSimultaneously()
        {
            ReaderWriterEntity rw1 = new ReaderWriterEntity();
            ReaderWriterEntity rw2 = new ReaderWriterEntity();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            this.InvokeSimultaneouslyAndWait( () => rw1.Write( 100, 100 ), () => rw2.Write( 100, 100 ) );
            stopwatch.Stop();
            Assert.Less( stopwatch.ElapsedMilliseconds, 150 );
        }

        [Test]
        public void ReaderLockAcquiredFromWriterMethod_DoesNotThrow()
        {
            ReaderWriterAttributeClass rw = new ReaderWriterAttributeClass();
            rw.Write( 0, () => rw.Read( () => { } ) );
        }

        [Test]
        public void ReaderLockAcquiredFromReaderMethod_DoesNotThrow()
        {
            ReaderWriterAttributeClass rw = new ReaderWriterAttributeClass();
            rw.Read(() => rw.Read(() => { }));
        }

        [Test]
        public void ReaderLockAcquiredFromUpgradeableMethod_DoesNotThrow()
        {
            ReaderWriterAttributeClass rw = new ReaderWriterAttributeClass();
            rw.UpgradeableRead(() => rw.Read(() => { }));
        }

        [Test]
        [ExpectedException(typeof(LockRecursionException))]
        public void UpgradeableLockLockAcquiredFromReadMethod_DoesThrow()
        {
            ReaderWriterAttributeClass rw = new ReaderWriterAttributeClass();
            rw.Read(() => rw.UpgradeableRead(() => { }));
        }

        [Test]
        [ExpectedException(typeof(LockRecursionException))]
        public void WriteLockLockAcquiredFromReadMethod_DoesThrow()
        {
            ReaderWriterAttributeClass rw = new ReaderWriterAttributeClass();
            rw.Read(() => rw.Write(0, () => { }));
        }

        [Test]
        public void ObserverAndReadFromWriteMethod_DoesNotThrow()
        {
            ReaderWriterAttributeClass rw = new ReaderWriterAttributeClass();
            rw.Write(1, () =>
                { 
                    rw.Observe( () => { } );
                    rw.Read( () => { } );
                } );
        }

        [Test]
        public void ReaderWriteWithObserverMethod_WhenInvoked_DoesNotThrow()
        {
            ReaderWriterWithObserverMethodClass rw = new ReaderWriterWithObserverMethodClass();

            rw.Write( 100, () => { } );

            //rw.Read( 100 );
        }

        [Test]
        public void ReaderWriterObserverEventTest()
        {
            ReaderWriterWithObserverEventClass rw = new ReaderWriterWithObserverEventClass();

            ManualResetEventSlim mrEvent = new ManualResetEventSlim( false );

            rw.OnWrite += mrEvent.Set;

            rw.Write( 100 );

            mrEvent.Wait();
        }

        [Test]
        [ExpectedException( typeof(LockNotHeldException) )]
        public void TestWriteWithoutLock()
        {
            new ReaderWriterEntity().WriteWithoutLock();
        }

        [Test]
        public void TestThreadSafeWriteWithoutLock()
        {
            new ReaderWriterEntity().ThreadSafeField = 4;
        }

        [Test]
        public void TestConstructor()
        {
            new ReaderWriterEntity( 4 );
        }

        [Test]
        public void TestConstructorDerived()
        {
            new ReaderWriterEntityDerived( 4 );
        }

        [Test]
        [ExpectedException( typeof(LockNotHeldException) )]
        public void TestReadTwoFields()
        {
            new ReaderWriterEntity().ReadTwoFields();
        }

        [Test]
        public void TestReadTwoFieldsWithReadLock()
        {
            new ReaderWriterEntity().ReadTwoFieldsWithReadLock();
        }

        [Test]
        public void TestReadTwoFieldsWithUpgradeableReadLock()
        {
            new ReaderWriterEntity().ReadTwoFieldsWithUpgredeableReadLock();
        }

        [Test]
        public void TestReadTwoFieldsWithWriteLock()
        {
            new ReaderWriterEntity().ReadTwoFieldsWithWriteLock();
        }

        [Test]
        [ExpectedException( typeof(LockNotHeldException) )]
        public void TestReadTwoFieldsIndirectly()
        {
            new ReaderWriterEntity().ReadTwoFieldsIndirecly();
        }

        [Test]
        public void TestReadTwoFieldsOneIsSafe()
        {
            new ReaderWriterEntity().ReadTwoFieldsOneIsSafe();
        }

        [Test]
        public void TestReadOneField()
        {
            new ReaderWriterEntity().ReadOneField();
        }

        [Test]
        public void AcquireWriteLockFromUpgradeableRead_DoesNotThrow()
        {
            ReaderWriterWithUpgradeableReadlockClass rw = new ReaderWriterWithUpgradeableReadlockClass();
            rw.ReadAndWrite( 2100 );
        }
    }

    [ReaderWriterSynchronized( CheckFieldAccess = true )]
    public class ReaderWriterEntity
    {
        protected int field, field2;
        [ThreadSafe] public int ThreadSafeField;

        public ReaderWriterEntity()
        {
        }

        public ReaderWriterEntity( int f )
        {
            this.field = f;
        }

        [ReaderLock]
        public int Read( int timespan = 0 )
        {
            Thread.Sleep( timespan );
            return this.field;
        }

        [WriterLock]
        public void Write( int value, int timespan = 0 )
        {
            Thread.Sleep( timespan );
            this.field = value;
        }

        [ReaderLock]
        public int ReadTwoFieldsWithReadLock()
        {
            return this.field + this.field2;
        }

        [WriterLock]
        public int ReadTwoFieldsWithWriteLock()
        {
            return this.field + this.field2;
        }

        [UpgradeableReaderLock]
        public int ReadTwoFieldsWithUpgredeableReadLock()
        {
            return this.field + this.field2;
        }

        public void WriteWithoutLock()
        {
            this.field = 0;
        }

        public int ReadTwoFields()
        {
            return this.field + this.field2;
        }

        public int ReadOneField()
        {
            return this.field;
        }

        public int ReadTwoFieldsIndirecly()
        {
            return this.ReadOneField() + this.ReadOneField();
        }

        public int ReadTwoFieldsOneIsSafe()
        {
            return this.field + this.ThreadSafeField;
        }
    }

    [ReaderWriterSynchronized]
    public class ReaderWriterWithObserverMethodClass
    {
        private int field;

        [ReaderLock]
        public int Read()
        {
            return this.field;
        }

        [ObserverLock]
        public void Observe( Action action )
        {
            action();
        }

        [WriterLock]
        public void Write( int value, Action action )
        {
            this.field = value;
            this.Observe( action );
        }

        [WriterLock]
        public void WriteThrowing( int value )
        {
            this.field = value;
            this.Read();
        }
    }

    [ReaderWriterSynchronized]
    public class ReaderWriterWithObserverEventClass
    {
        private int field;

        [ReaderLock]
        public int Read()
        {
            return this.field;
        }

        [ObserverLock]
        public event Action OnWrite;

        [WriterLock]
        public void Write( int value )
        {
            this.field = value;
            if ( this.OnWrite != null )
            {
                this.OnWrite();
            }
        }
    }

    [ReaderWriterSynchronized]
    public class ReaderWriterWithUpgradeableReadlockClass
    {
        private int field;

        [UpgradeableReaderLock]
        public int ReadAndWrite( int value )
        {
            this.Write( value );
            return this.field;
        }


        [WriterLock]
        public void Write( int value )
        {
            this.field = value;
        }
    }

    public class ReaderWriterEntityDerived : ReaderWriterEntity
    {
        public ReaderWriterEntityDerived( int f )
            : base( f )
        {
            this.field = 5;
        }
    }

    [ReaderWriterSynchronized]
    public class ReaderWriterAttributeClass
    {
        private int field;

        [ObserverLock]
        public int Observe(Action action)
        {
            action();
            int value = this.field;
            return value;
        }

        [UpgradeableReaderLock]
        public int UpgradeableRead(Action action)
        {
            action();
            int value = this.field;
            return value;
        }

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
}