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
            HistoryTracker historyTracker = new HistoryTracker();
            SimpleTrackedObject sto1 = new SimpleTrackedObject();
            SimpleTrackedObject sto2 = new SimpleTrackedObject();

            historyTracker.Track(sto1).Track(sto2);

            sto1.ChangeValues(1, 2, 3);

            sto2.ChangeValues(2, 3, 4);

            historyTracker.Undo();

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
            HistoryTracker historyTracker = new HistoryTracker();
            SimpleTrackedObject sto1 = new SimpleTrackedObject();
            SimpleTrackedObject sto2 = new SimpleTrackedObject();

            historyTracker.Track(sto1).Track(sto2);

            sto1.ChangeValues(1, 2, 3);

            sto2.ChangeValues(2, 3, 4);

            ((ITrackedObject)sto1).Tracker.Undo();

            Assert.AreEqual(0, sto1.P1);
            Assert.AreEqual(0, sto1.P2);
            Assert.AreEqual(0, sto1.P3);

            historyTracker.Undo();

            Assert.AreEqual(1, sto1.P1);
            Assert.AreEqual(2, sto1.P2);
            Assert.AreEqual(3, sto1.P3);

            Assert.AreEqual(2, sto2.P1);
            Assert.AreEqual(3, sto2.P2);
            Assert.AreEqual(4, sto2.P3);

            historyTracker.Undo();

            Assert.AreEqual(0, sto2.P1);
            Assert.AreEqual(0, sto2.P2);
            Assert.AreEqual(0, sto2.P3);
        }

        [Test]
        public void GlobalTrackerUndo_RestoresLocalTrackerHistory()
        {
            HistoryTracker historyTracker = new HistoryTracker();
            SimpleTrackedObject sto1 = new SimpleTrackedObject();
            SimpleTrackedObject sto2 = new SimpleTrackedObject();

            historyTracker.Track(sto1).Track(sto2);

            sto1.ChangeValues(1, 2, 3);

            ObjectTracker.AddRestorePoint(sto1, "r1");

            sto1.ChangeValues(4, 5, 6);

            sto1.ChangeValues(7, 8, 9);


            sto2.ChangeValues(2, 3, 4);


            ObjectTracker.UndoTo(sto1, "r1");

            Assert.AreEqual(1, sto1.P1);
            Assert.AreEqual(2, sto1.P2);
            Assert.AreEqual(3, sto1.P3);

            historyTracker.Undo();

            Assert.AreEqual(7, sto1.P1);
            Assert.AreEqual(8, sto1.P2);
            Assert.AreEqual(9, sto1.P3);

            Assert.AreEqual(2, sto2.P1);
            Assert.AreEqual(3, sto2.P2);
            Assert.AreEqual(4, sto2.P3);

            ((ITrackedObject)sto1).Tracker.Undo();

            Assert.AreEqual(4, sto1.P1);
            Assert.AreEqual(5, sto1.P2);
            Assert.AreEqual(6, sto1.P3);
        }

        [Test]
        public void HistoryTracker_WhenMaxCountSet_MaxCountIsLimited()
        {
            HistoryTracker historyTracker = new HistoryTracker();
            SimpleTrackedObject sto1 = new SimpleTrackedObject();

            historyTracker.Track(sto1);

            historyTracker.MaximumOperationsCount = 5;

            sto1.ChangeValues(1, 2, 3);
            sto1.ChangeValues(2, 5, 6);
            sto1.ChangeValues(3, 8, 9);
            sto1.ChangeValues(4, 2, 3);
            sto1.ChangeValues(5, 5, 6);
            sto1.ChangeValues(6, 8, 9);
            sto1.ChangeValues(7, 2, 3);
            sto1.ChangeValues(8, 5, 6);
            sto1.ChangeValues(9, 8, 9);

            for (int i = 0; i < 9; i++) historyTracker.Undo();

            Assert.AreEqual(4, sto1.P1);
            Assert.AreEqual(2, sto1.P2);
            Assert.AreEqual(3, sto1.P3);
        }
    }
}
