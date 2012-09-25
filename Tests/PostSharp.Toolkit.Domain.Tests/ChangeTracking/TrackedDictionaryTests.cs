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
    public class TrackedDictionaryTests
    {
        [Test]
        public void SimpleTrackedDictionaryAddTest()
        {
            TrackedDictionary<int, string> tc = new TrackedDictionary<int, string>(CollectionTrackingStrategy.TrackOnlyCollectionOperations);

            tc.Tracker.Track();

            tc.Add(0, "0");
            tc.Add(1, "1");
            tc.Add(2, "2");
            tc.Add(3, "3");
            tc.Add(4, "4");
            tc.Add(5, "5");

            ObjectTracker.SetRestorePoint(tc, "After 5");

            tc.Add(6, "6");
            tc.Add(7, "7");
            tc.Add(8, "8");
            tc.Add(9, "9");
            tc.Add(10, "10");

            ObjectTracker.UndoTo(tc, "After 5");

            Assert.AreEqual( 5, tc.Last().Key );

            tc.Tracker.Redo();

            Assert.AreEqual(6, tc.Last().Key);

            tc.Tracker.Undo();

            Assert.AreEqual(5, tc.Last().Key);

            tc.Tracker.Undo();

            Assert.AreEqual(4, tc.Last().Key);

            tc.Remove( 1 );
            Assert.IsFalse( tc.ContainsKey( 1 ) );

            tc.Tracker.Undo();

            Assert.AreEqual( "1", tc[1] );
        }
    }
}