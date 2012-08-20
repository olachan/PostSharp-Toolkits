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

            to.ChangeValues( 1,2,3 );

            ((ITrackedObject)to).Undo();

            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);
        }

        [Test]
        public void SimpleUndoRedoTest()
        {
            TrackedObject to = new TrackedObject();
            var sot = (ITrackedObject)to;

            to.ChangeValues(1, 2, 3);

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
        public void MultipleUndoRedoTest()
        {
            TrackedObject to = new TrackedObject();
            var sot = (ITrackedObject)to;


            to.ChangeValues(1, 2, 3);

            sot.Undo();

            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);

            sot.Redo();

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);

            to.P1 = 2;
            to.P1 = 3;
            to.P1 = 4;

            sot.Undo(); Assert.AreEqual(3, to.P1);

            sot.Undo(); Assert.AreEqual(2, to.P1);

            sot.Undo(); Assert.AreEqual(1, to.P1);

            sot.Undo(); Assert.AreEqual(0, to.P1);

            sot.Undo(); Assert.AreEqual(0, to.P1); // empty undo collection

            sot.Redo(); Assert.AreEqual(1, to.P1);

            sot.Redo(); Assert.AreEqual(2, to.P1);

            sot.Redo(); Assert.AreEqual(3, to.P1);

            sot.Redo(); Assert.AreEqual(4, to.P1);

            sot.Redo(); Assert.AreEqual(4, to.P1); // empty redo collection

            sot.Undo(); Assert.AreEqual(3, to.P1);

            sot.Redo(); Assert.AreEqual(4, to.P1);
        }

        [Test]
        public void NamedRestorePointTest()
        {
            TrackedObject to = new TrackedObject();
            var sot = (ITrackedObject)to;

            to.ChangeValues(1, 2, 3);

            sot.AddNamedRestorePoint("s1");

            to.ChangeValues(4, 5, 6);

            to.ChangeValues(7, 8, 9);

            to.ChangeValues(10, 11, 12);

            sot.RestoreNamedRestorePoint("s1");

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);

            sot.Redo(); // TODO: should redo after restoring named point work this way? 

            Assert.AreEqual(4, to.P1);
            Assert.AreEqual(5, to.P2);
            Assert.AreEqual(6, to.P3);

            sot.Undo(); // TODO: what shoul happen here?

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);
        }

        [Test]
        public void RedoAfterNamedRestorePointTest_RestoresPoint()
        {
            TrackedObject to = new TrackedObject();
            var sot = (ITrackedObject)to;

            to.ChangeValues(1, 2, 3);

            sot.AddNamedRestorePoint("s1");

            to.ChangeValues(4, 5, 6);

            to.ChangeValues(7, 8, 9);

            to.ChangeValues(10, 11, 12);

            sot.RestoreNamedRestorePoint("s1");

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);

            sot.Redo();

            Assert.AreEqual(4, to.P1);
            Assert.AreEqual(5, to.P2);
            Assert.AreEqual(6, to.P3);

            sot.Redo();

            Assert.AreEqual(7, to.P1);
            Assert.AreEqual(8, to.P2);
            Assert.AreEqual(9, to.P3);

            sot.RestoreNamedRestorePoint("s1");

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);
        }

        [Test]
        public void MultipleNamedRestorePointsTest()
        {
            TrackedObject to = new TrackedObject();
            var sot = (ITrackedObject)to;

            to.ChangeValues(1, 2, 3);

            sot.AddNamedRestorePoint("s1");

            to.ChangeValues(4, 5, 6);

            to.ChangeValues(7, 8, 9);

            sot.AddNamedRestorePoint("s1");

            to.ChangeValues(10, 11, 12);

            to.ChangeValues(1, 2, 3);

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

        public void ChangeValues(int? p1 = null, int? p2 = null, int? p3 = null)
        {
            this.P1 = p1.HasValue ? p1.Value : this.P1;
            this.P2 = p2.HasValue ? p2.Value : this.P2;
            this.P3 = p3.HasValue ? p3.Value : this.P3;
        }
    }
}