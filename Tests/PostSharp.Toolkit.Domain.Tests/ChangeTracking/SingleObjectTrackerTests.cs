#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using NUnit.Framework;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.Tests.ChangeTracking
{
    [TestFixture]
    public class SingleObjectTrackerTests
    {
        [Test]
        public void SimpleUndoTest()
        {
            SimpleTrackedObject to = new SimpleTrackedObject();

            to.ChangeValues( 1,2,3 );

            ((ITrackedObject)to).Undo();

            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);
        }

        [Test]
        public void SimpleUndoRedoTest()
        {
            SimpleTrackedObject to = new SimpleTrackedObject();
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
            SimpleTrackedObject to = new SimpleTrackedObject();
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
            SimpleTrackedObject to = new SimpleTrackedObject();
            var sot = (ITrackedObject)to;

            to.ChangeValues(1, 2, 3);

            sot.AddRestorePoint("s1");

            to.ChangeValues(4, 5, 6);

            to.ChangeValues(7, 8, 9);

            to.ChangeValues(10, 11, 12);

            sot.UndoToRestorePoint("s1");

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
            SimpleTrackedObject to = new SimpleTrackedObject();
            var sot = (ITrackedObject)to;

            to.ChangeValues(1, 2, 3);

            sot.AddRestorePoint("s1");

            to.ChangeValues(4, 5, 6);

            to.ChangeValues(7, 8, 9);

            to.ChangeValues(10, 11, 12);

            sot.UndoToRestorePoint("s1");

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

            sot.UndoToRestorePoint("s1");

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);
        }

        [Test]
        public void MultipleNamedRestorePointsTest()
        {
            SimpleTrackedObject to = new SimpleTrackedObject();
            var sot = (ITrackedObject)to;

            to.ChangeValues(1, 2, 3);

            sot.AddRestorePoint("s1");

            to.ChangeValues(4, 5, 6);

            to.ChangeValues(7, 8, 9);

            sot.AddRestorePoint("s1");

            to.ChangeValues(10, 11, 12);

            to.ChangeValues(1, 2, 3);

            sot.UndoToRestorePoint("s1");

            Assert.AreEqual(7, to.P1);
            Assert.AreEqual(8, to.P2);
            Assert.AreEqual(9, to.P3);

            sot.UndoToRestorePoint("s1");

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);

            sot.Undo();

            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);
        }

        [Test]
        public void SimpleUndoRedoWithAttributesTest()
        {
            TrackedObject to = new TrackedObject();
            var sot = (ITrackedObject)to;

            to.ChangeValuesTracked(1, 2, 3);

            sot.Undo();
            sot.Undo();
            
            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);

            sot.Redo();
            sot.Redo();

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);

            to.ChangeValuesNotTracked( 0, 0, 0 );
            to.ChangeValuesNotTracked(1, 2, 3);

            sot.Undo();

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(0, to.P3);

            sot.Redo();

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);
        }

        [Test]
        public void NamedChunkTest()
        {
            SimpleTrackedObject to = new SimpleTrackedObject();
            var sot = (ITrackedObject)to;

            using ( sot.Tracker.StartAtomicOperation() )
            {
                to.ChangeValues(4, 5, 6);
                to.ChangeValues(7, 8, 9);
                to.ChangeValues(10, 11, 12);
            }

            sot.Undo();
            Assert.AreEqual(0, to.P1);
            Assert.AreEqual(0, to.P2);
            Assert.AreEqual(0, to.P3);

            sot.Redo();

            Assert.AreEqual(10, to.P1);
            Assert.AreEqual(11, to.P2);
            Assert.AreEqual(12, to.P3);

            to.ChangeValues(1, 2, 3);
            
            sot.Undo();

            Assert.AreEqual(10, to.P1);
            Assert.AreEqual(11, to.P2);
            Assert.AreEqual(12, to.P3);

            sot.Redo();

            Assert.AreEqual(1, to.P1);
            Assert.AreEqual(2, to.P2);
            Assert.AreEqual(3, to.P3);
        }
    }

    [TrackedObject]
    public class SimpleTrackedObject
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

    [TrackedObject]
    public class TrackedObject
    {
        public int P1 { get; set; }

        public int P2 { get; set; }

        public int P3 { get; [ForceChangeTrackingOperation]set; }

        public void ChangeValuesTracked(int? p1 = null, int? p2 = null, int? p3 = null)
        {
            if (p1.HasValue)
            {
                this.P1 = p1.Value;
            }

            if(p2.HasValue)
            {
                this.P2 = p2.Value;
            }
            
            if (p3.HasValue)
            {
                this.P3 = p3.Value;
            }
        }

        [NoAutomaticChangeTrackingOperation]
        public void ChangeValuesNotTracked(int? p1 = null, int? p2 = null, int? p3 = null)
        {
            this.ChangeValuesTracked( p1, p2, p3 );
        }
    }
}