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

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class MemberwiseCloneTests
    {
        // Failing test: known issue of postsharp - implicit aspect shearing after invoking MemberwiseClone
        [Test]
        public void MemberwiseCloneTest()
        {
            ReaderWriterWithClone rw1 = new ReaderWriterWithClone();
            ReaderWriterWithClone rw2 = rw1.Clone();

            Barrier barrier = new Barrier( 2 );

            Task t1 = new Task( () => rw1.Write( 0, barrier.SignalAndWait ) );
            Task t2 = new Task( () => rw2.Write( 0, barrier.SignalAndWait ) );

            t1.Start();
            t2.Start();

            Assert.True( Task.WaitAll( new[] { t1, t2 }, 1000 ) );
        }

        [Test]
        public void MemberwiseCloneWithInitializationTest()
        {
            ReaderWriterWithClone rw1 = new ReaderWriterWithClone();
            ReaderWriterWithClone rw2 = rw1.InitilizedClone();

            Barrier barrier = new Barrier(2);

            Task t1 = new Task(() => rw1.Write(0, barrier.SignalAndWait));
            Task t2 = new Task(() => rw2.Write(0, barrier.SignalAndWait));

            t1.Start();
            t2.Start();

            Assert.True(Task.WaitAll(new[] { t1, t2 }, 1000));
        }

        [Test]
        public void TwoInstanceTest()
        {
            ReaderWriterWithClone rw1 = new ReaderWriterWithClone();
            ReaderWriterWithClone rw2 = new ReaderWriterWithClone();

            Barrier barrier = new Barrier( 2 );

            Task t1 = new Task( () => rw1.Write( 0, barrier.SignalAndWait ) );
            Task t2 = new Task( () => rw2.Write( 0, barrier.SignalAndWait ) );

            t1.Start();
            t2.Start();

            Assert.True( Task.WaitAll( new[] { t1, t2 }, 1000 ) );
        }
    }

    [ReaderWriterSynchronized]
    public class ReaderWriterWithClone
    {
        private int field;

        [ReaderLock]
        public int Read()
        {
            return this.field;
        }

        [WriterLock]
        public void Write( int value, Action action )
        {
            action();
            this.field = value;
        }

        protected virtual void InitializeAspects()
        {
            
        }

        public ReaderWriterWithClone Clone()
        {
            return (ReaderWriterWithClone)this.MemberwiseClone();
        }

        public ReaderWriterWithClone InitilizedClone()
        {
            var clone = (ReaderWriterWithClone)this.MemberwiseClone();
            clone.InitializeAspects();
            return clone;
        }
    }
}