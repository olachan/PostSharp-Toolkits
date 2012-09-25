using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using PostSharp.Toolkit.Domain;
using PostSharp.Toolkit.Threading;

namespace PostSharp.Toolkit.Integration.Tests
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class ChangeTrackingThreadingIntegrationTests
    {
        [Test]
        public void OnThreadUnsafeObject_WhenUndoPerformedFormTheSameThread_Success()
        {
            ThreadUnsafeTrackedObject root = new ThreadUnsafeTrackedObject();

            var restorePoint = ObjectTracker.SetRestorePoint(root);

            root.S1 = "asd";
            root.S2 = "dfg";

            ObjectTracker.UndoTo(root, restorePoint);

            Assert.AreEqual(null, root.S1);
            Assert.AreEqual(null, root.S2);
        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void OnThreadUnsafeObject_WhenUndoPerformedFormOtherThread_ExceptionIsThrown()
        {
            ThreadUnsafeTrackedObject root = new ThreadUnsafeTrackedObject();

            var restorePoint = ObjectTracker.SetRestorePoint(root);

            root.S1 = "asd";
            root.S2 = "dfg";

            try
            {
                Task.Factory.StartNew(() => ObjectTracker.UndoTo(root, restorePoint)).Wait();
            }
            catch (AggregateException aggrExc)
            {
                if (aggrExc.InnerExceptions.Count == 1) throw aggrExc.InnerExceptions[0];
                else throw;
            }
        }

        [Test]
        public void OnThreadUnsafeObject_WhenChangesPerformedOnNestedAndUndoPerformedFormTheSameThread_Success()
        {
            ThreadUnsafeTrackedObject root = new ThreadUnsafeTrackedObject();
            root.Nested = new NestedTrackedObject();

            var restorePoint = ObjectTracker.SetRestorePoint(root);

            root.Nested.S1 = "asd";
            root.Nested.S2 = "dfg";

            ObjectTracker.UndoTo( root, restorePoint );

            Assert.AreEqual(null, root.Nested.S1);
            Assert.AreEqual(null, root.Nested.S1);
        }

        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void OnThreadUnsafeObject_WhenChangesPerformedOnNestedAndUndoPerformedFormOtherThread_ExceptionIsThrown()
        {
            ThreadUnsafeTrackedObject root = new ThreadUnsafeTrackedObject();
            root.Nested = new NestedTrackedObject();

            var restorePoint = ObjectTracker.SetRestorePoint(root);

            root.Nested.S1 = "asd";
            root.Nested.S2 = "dfg";

            try
            {
                Task.Factory.StartNew(() => ObjectTracker.UndoTo(root, restorePoint)).Wait();
            }
            catch (AggregateException aggrExc)
            {
                if (aggrExc.InnerExceptions.Count == 1) throw aggrExc.InnerExceptions[0];
                else throw;
            }
        }

        [Test]
        [Ignore] // TODO resolve interaction between ReaderWriterSynchronized and TrackedObject
        public void OnReaderWriterSynchronizedObject_WhenUndoPerformed_LocksAreRespected()
        {
            var testObject = new ReaderWriterSynchronizedTrackedObject();

            testObject.S1 = "123";

            var restorePoint = ObjectTracker.SetRestorePoint( testObject );

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Task writerLockAcquiredTask = Task.Factory.StartNew( () => testObject.S1 = "sad" );

            ObjectTracker.UndoTo( testObject, restorePoint );

            stopwatch.Stop();

            writerLockAcquiredTask.Wait();

            Assert.IsTrue( stopwatch.ElapsedMilliseconds >= 200 );
        }
    }

    // ReSharper restore InconsistentNaming 

    [TrackedObject(true)]
    [ThreadUnsafeObject(ThreadUnsafePolicy.ThreadAffined, CheckFieldAccess = true)]
    public class ThreadUnsafeTrackedObject
    {
        public string S1 { get; set; }

        public string S2 { get; set; }

        [NestedTrackedObject]
        public NestedTrackedObject Nested { get; set; }
    }

    [TrackedObject(true)]
    [ThreadUnsafeObject(ThreadUnsafePolicy.ThreadAffined, CheckFieldAccess = true)]
    public class NestedTrackedObject
    {
        public string S1 { get; set; }

        public string S2 { get; set; }
    }

    [TrackedObject(true)]
    [ReaderWriterSynchronized]
    public class ReaderWriterSynchronizedTrackedObject
    {
        private string s2;

        private string s1;

        [ReaderLock]
        public string GetS2(Action action)
        {
            action();
            return s2;
        }

        [WriterLock]
        public void SetS2(string s, Action action)
        {
            action();
            s2 = s;
        }

        public string S1
        {
            [ReaderLock]
            get
            {
                return this.s1;
            }
            [WriterLock]
            set
            {
                Thread.Sleep( 200 );
                this.s1 = value;
            }
        }
    }
}