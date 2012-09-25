#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Linq;
using NUnit.Framework;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.Tests.ChangeTracking
{
    [TestFixture]
    public class TrackedCollectionTests
    {
        [Test]
        public void Add_WhenCalledAndReverted_AddsAndReverts()
        {
            TrackedCollection<int> tc = new TrackedCollection<int>();

            tc.Tracker.Track();

            tc.Add( 0 );
            tc.Add(1);
            tc.Add(2);
            tc.Add(3);
            tc.Add(4);
            tc.Add(5);

            ObjectTracker.SetRestorePoint(tc, "After 5");

            tc.Add(6);
            tc.Add(7);
            tc.Add(8);
            tc.Add(9);
            tc.Add(10);

            ObjectTracker.UndoTo(tc, "After 5");

            Assert.AreEqual(5, tc.Last());

            tc.Tracker.Redo();

            Assert.AreEqual(6, tc.Last());

            tc.Tracker.Undo();

            Assert.AreEqual(5, tc.Last());

            tc.Tracker.Undo();

            Assert.AreEqual(4, tc.Last());
        }

        [Test]
        public void Remove_WhenCalledAndReverted_RemovesAndReverts()
        {
            TrackedCollection<int> tc = new TrackedCollection<int>();

            tc.Tracker.Track();

            tc.Add(0);
            tc.Add(1);
            tc.Add(2);

            tc.Remove(0);
            tc.Remove(1);

            Assert.IsFalse(tc.Contains(0));
            Assert.IsFalse(tc.Contains(1));

            tc.Tracker.Undo();

            Assert.IsTrue(tc.Contains(1));
        }

        [Test]
        public void RemoveAt_WhenCalledAndReverted_RemovesAndReverts()
        {
            TrackedCollection<int> tc = new TrackedCollection<int>();

            tc.Tracker.Track();

            tc.Add(0);
            tc.Add(1);
            tc.Add(2);

            tc.RemoveAt(1);

            Assert.IsFalse(tc.Contains(1));
            Assert.IsTrue(tc[1] == 2);

            tc.Tracker.Undo();

            Assert.IsTrue(tc[1] == 1);
            Assert.IsTrue(tc.Contains(1));
        }

        [Test]
        public void Undo_WhenCalledAfterClear_RevertsClear()
        {
            TrackedCollection<int> tc = new TrackedCollection<int>();

            tc.Tracker.Track();

            tc.Add(0);
            tc.Add(1);
            tc.Add(2);

            tc.Clear();

            Assert.IsTrue(tc.Count == 0);

            tc.Tracker.Undo();

            Assert.IsTrue(tc.Count == 3);
            Assert.IsTrue(tc[0] == 0);
            Assert.IsTrue(tc[1] == 1);
            Assert.IsTrue(tc[2] == 2);
        }

        [Test]
        public void Insert_WhenCalledAndReverted_InsertsAndReverts()
        {
            TrackedCollection<int> tc = new TrackedCollection<int>();

            tc.Tracker.Track();

            tc.Add(0);
            tc.Add(1);
            tc.Add(2);

            tc.Insert(0, 10);

            Assert.IsTrue(tc[0] == 10);

            tc.Tracker.Undo();

            Assert.IsTrue(tc[0] == 0);
            Assert.IsFalse(tc.Contains( 10 ));
        }
    }
}