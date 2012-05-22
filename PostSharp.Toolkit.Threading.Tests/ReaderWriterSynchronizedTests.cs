using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using PostSharp.Toolkit.Threading.Synchronization;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class ReaderWriterSynchronizedTests
    {
        protected void InvokeSimultaneouslyAndWait(Action action1, Action action2)
        {
            try
            {
                var t1 = new Task(action1);
                var t2 = new Task(action2);
                t1.Start();
                Thread.Sleep(20); // Ensure deterministic order
                t2.Start();
                t1.Wait();
                t2.Wait();
            }
            catch (AggregateException aggregateException)
            {
                Thread.Sleep(500); //Make sure the second running task is over as well
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    throw aggregateException.InnerException;
                }
                throw;
            }
        }

        [Test]
        public void TwoReadersCanRead()
        {
            var rw = new ReaderWriterEntity();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            InvokeSimultaneouslyAndWait(() => rw.Read(100), () => rw.Read(100));
            stopwatch.Stop();
            Assert.Less(stopwatch.ElapsedMilliseconds, 150);
        }

        [Test]
        public void WhenWriterWritesReaderWaits()
        {
            var rw = new ReaderWriterEntity();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            InvokeSimultaneouslyAndWait(() => rw.Write(100, 101), () => rw.Read(101));
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 200);
        }

        [Test]
        public void WhenReaderReadsWriterWaits()
        {
            var rw = new ReaderWriterEntity();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            InvokeSimultaneouslyAndWait(() => rw.Write(100, 101), () => rw.Read(101));
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 200);
        }

        [Test]
        public void WhenWriterWritesWriterWaits()
        {
            var rw = new ReaderWriterEntity();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            InvokeSimultaneouslyAndWait(() => rw.Write(100, 101), () => rw.Write(100, 101));
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 200);
        }

        [Test]
        public void TwoObjectsCanWriteSimultaneously()
        {
            var rw1 = new ReaderWriterEntity();
            var rw2 = new ReaderWriterEntity();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            InvokeSimultaneouslyAndWait(() => rw1.Write(100, 100), () => rw2.Write(100, 100));
            stopwatch.Stop();
            Assert.Less(stopwatch.ElapsedMilliseconds, 150);
        }
    }

    [ReaderWriterSynchronized]
    public class ReaderWriterEntity
    {
        private int field;

        [ReadLock]
        public int Read(int timespan = 0)
        {
            Thread.Sleep(timespan);
            return this.field;
        }

        [WriteLock]
        public void Write(int value, int timespan = 0)
        {
            Thread.Sleep(timespan);
            this.field = value;
        }
    }
}