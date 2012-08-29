using NUnit.Framework;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.Tests.ChangeTracking
{
    [TestFixture]
    public class GlobalTrackerTests
    {
        [Test]
        public void SimpleGlobalTrackerTest()
        {
            GlobalTracker globalTracker = new GlobalTracker();
            SimpleTrackedObject sto1 = new SimpleTrackedObject();
            SimpleTrackedObject sto2 = new SimpleTrackedObject();

            globalTracker.Track( (ITrackedObject)sto1 ).Track( (ITrackedObject)sto2 );

            sto1.ChangeValues(1, 2, 3);

            sto2.ChangeValues( 2,3,4 );

            globalTracker.Undo();

            Assert.AreEqual(1, sto1.P1);
            Assert.AreEqual(2, sto1.P2);
            Assert.AreEqual(3, sto1.P3);

            Assert.AreEqual(0, sto2.P1);
            Assert.AreEqual(0, sto2.P2);
            Assert.AreEqual(0, sto2.P3);
        }

        [Test]
        public void SimpleGlobalTrackerMixedWithLocalTrackerTest()
        {
            GlobalTracker globalTracker = new GlobalTracker();
            SimpleTrackedObject sto1 = new SimpleTrackedObject();
            SimpleTrackedObject sto2 = new SimpleTrackedObject();

            globalTracker.Track((ITrackedObject)sto1).Track((ITrackedObject)sto2);

            sto1.ChangeValues(1, 2, 3);

            sto2.ChangeValues(2, 3, 4);

            ((ITrackedObject)sto1).Undo();

            Assert.AreEqual(0, sto1.P1);
            Assert.AreEqual(0, sto1.P2);
            Assert.AreEqual(0, sto1.P3);

            globalTracker.Undo();

            Assert.AreEqual(1, sto1.P1);
            Assert.AreEqual(2, sto1.P2);
            Assert.AreEqual(3, sto1.P3);

            Assert.AreEqual(2, sto2.P1);
            Assert.AreEqual(3, sto2.P2);
            Assert.AreEqual(4, sto2.P3);

            globalTracker.Undo();

            Assert.AreEqual(0, sto2.P1);
            Assert.AreEqual(0, sto2.P2);
            Assert.AreEqual(0, sto2.P3);
        }

        [Test]
        public void GlobalTrackerUndo_RestoresLocalTrackerHistory()
        {
            GlobalTracker globalTracker = new GlobalTracker();
            SimpleTrackedObject sto1 = new SimpleTrackedObject();
            SimpleTrackedObject sto2 = new SimpleTrackedObject();

            globalTracker.Track((ITrackedObject)sto1).Track((ITrackedObject)sto2);

            sto1.ChangeValues(1, 2, 3);

            ((ITrackedObject)sto1).AddRestorePoint( "r1" );

            sto1.ChangeValues(4, 5, 6);

            sto1.ChangeValues(7, 8, 9);


            sto2.ChangeValues(2, 3, 4);

            ((ITrackedObject)sto1).UndoToRestorePoint( "r1" );

            Assert.AreEqual(1, sto1.P1);
            Assert.AreEqual(2, sto1.P2);
            Assert.AreEqual(3, sto1.P3);

            globalTracker.Undo();

            Assert.AreEqual(7, sto1.P1);
            Assert.AreEqual(8, sto1.P2);
            Assert.AreEqual(9, sto1.P3);

            Assert.AreEqual(2, sto2.P1);
            Assert.AreEqual(3, sto2.P2);
            Assert.AreEqual(4, sto2.P3);

            ((ITrackedObject)sto1).Undo();

            Assert.AreEqual(4, sto1.P1);
            Assert.AreEqual(5, sto1.P2);
            Assert.AreEqual(6, sto1.P3);
        }
    }
}
