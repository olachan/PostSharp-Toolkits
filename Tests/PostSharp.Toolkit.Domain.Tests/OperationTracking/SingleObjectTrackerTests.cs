#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using NUnit.Framework;

using PostSharp.Toolkit.Domain.OperationTracking;

namespace PostSharp.Toolkit.Domain.Tests.OperationTracking
{
    [TestFixture]
    public class SingleObjectTrackerTests
    {
        [Test]
        public void SimpleUndoTest()
        {
            TrackedObject to = new TrackedObject();
            SingleObjectTracker sot = new SingleObjectTracker((IOperationTrackable)to);

            sot.AddObjectSnapshot();

            to.P1 = 1;
            to.P2 = 2;
            to.P3 = 3;

            sot.Undo();

            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);
        }

        [Test]
        public void SimpleUndoRedoTest()
        {
            TrackedObject to = new TrackedObject();
            SingleObjectTracker sot = new SingleObjectTracker((IOperationTrackable)to);

            sot.AddObjectSnapshot();

            to.P1 = 1;
            to.P2 = 2;
            to.P3 = 3;

            sot.Undo();

            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);

            sot.Redo();

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);
        }

        [Test]
        public void NamedRestorePointTest()
        {
            TrackedObject to = new TrackedObject();
            SingleObjectTracker sot = new SingleObjectTracker((IOperationTrackable)to);

            sot.AddObjectSnapshot();

            to.P1 = 1;
            to.P2 = 2;
            to.P3 = 3;

            sot.AddNamedSnapshot( "s1" );

            to.P1 = 4;
            to.P2 = 5;
            to.P3 = 6;

            sot.AddObjectSnapshot();

            to.P1 = 7;
            to.P2 = 8;
            to.P3 = 9;

            sot.AddObjectSnapshot();

            to.P1 = 10;
            to.P2 = 11;
            to.P3 = 12;

            sot.AddObjectSnapshot();

            sot.RestoreNamedRestorePoint("s1");

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);
        }

        [Test]
        public void MultipleNamedRestorePointsTest()
        {
            TrackedObject to = new TrackedObject();
            SingleObjectTracker sot = new SingleObjectTracker((IOperationTrackable)to);

            sot.AddObjectSnapshot();

            to.P1 = 1;
            to.P2 = 2;
            to.P3 = 3;

            sot.AddNamedSnapshot("s1");

            to.P1 = 4;
            to.P2 = 5;
            to.P3 = 6;

            sot.AddObjectSnapshot();

            to.P1 = 7;
            to.P2 = 8;
            to.P3 = 9;

            sot.AddNamedSnapshot("s1");

            to.P1 = 10;
            to.P2 = 11;
            to.P3 = 12;

            sot.AddObjectSnapshot();

            to.P1 = 1;
            to.P2 = 2;
            to.P3 = 3;

            sot.AddObjectSnapshot();

            sot.RestoreNamedRestorePoint("s1");

            Assert.AreEqual(7, to.P1);
            Assert.AreEqual(8, to.P2);
            Assert.AreEqual(9, to.P3);

            sot.RestoreNamedRestorePoint("s1");

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);

            sot.Undo();

            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);
        }
    }

    [TrackedObject]
    public class TrackedObject
    {
        public int P1 { get; set; }

        public int P2 { get; set; }

        public int P3 { get; set; }
    }
}