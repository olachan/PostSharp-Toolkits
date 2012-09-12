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
    public class AggregateTrackerTests
    {
        [Test]
        public void SimpleAggregateTrackerTest()
        {
            AggregateTrackedObject root = new AggregateTrackedObject();

            root.DependentTrackedObject = new SimpleTrackedObject();

            var to = (ITrackedObject)root;
            root.ChangeValues(1, 2, 3);

            to.Tracker.Undo();

            Assert.AreEqual(0, root.P1);
            Assert.AreEqual(0, root.P2);
            Assert.AreEqual(0, root.P3);

            root.DependentTrackedObject.ChangeValues(1, 2, 3);

            to.Tracker.Undo();

            Assert.AreEqual(0, root.P1);
            Assert.AreEqual(0, root.P2);
            Assert.AreEqual(0, root.P3);

            Assert.AreEqual(0, root.DependentTrackedObject.P1);
            Assert.AreEqual(0, root.DependentTrackedObject.P2);
            Assert.AreEqual(0, root.DependentTrackedObject.P3);
        }

        [Test]
        public void AggregateMethodChangingDependentTest()
        {
            AggregateTrackedObject root = new AggregateTrackedObject();

            root.DependentTrackedObject = new SimpleTrackedObject();

            var to = (ITrackedObject)root;

            root.ChangeValuesWithDependent(1, 2, 3);

            to.Tracker.Undo();

            Assert.AreEqual(0, root.P1);
            Assert.AreEqual(0, root.P2);
            Assert.AreEqual(0, root.P3);

            Assert.AreEqual(0, root.DependentTrackedObject.P1);
            Assert.AreEqual(0, root.DependentTrackedObject.P2);
            Assert.AreEqual(0, root.DependentTrackedObject.P3);
        }

        [Test]
        public void LargeOperationTest()
        {
            AggregateTrackedObject root = new AggregateTrackedObject();

            root.DependentTrackedObject = new SimpleTrackedObject();

            var to = (ITrackedObject)root;

            using (ObjectTracker.StartAtomicOperation(to, "name"))
            {
                root.ChangeValuesWithDependent(1, 2, 3);
                root.ChangeValues(3, 4, 5);
                root.DependentTrackedObject.ChangeValues(6, 7, 8);
            }

            to.Tracker.Undo();

            Assert.AreEqual(0, root.P1);
            Assert.AreEqual(0, root.P2);
            Assert.AreEqual(0, root.P3);

            Assert.AreEqual(0, root.DependentTrackedObject.P1);
            Assert.AreEqual(0, root.DependentTrackedObject.P2);
            Assert.AreEqual(0, root.DependentTrackedObject.P3);
        }

        [Test]
        public void ReattachDependentObject_IfObjectIsUnchanged_RestoresAggregateTracker()
        {
            AggregateTrackedObject root = new AggregateTrackedObject();

            var dependentObject = new SimpleTrackedObject();
            root.DependentTrackedObject = dependentObject;

            dependentObject.ChangeValues(1, 2, 3);

            root.DependentTrackedObject = null;

            dependentObject.ChangeValues(4, 5, 6);

            ((ITrackedObject)dependentObject).Tracker.Undo();

            ((ITrackedObject)root).Tracker.Undo();
            ((ITrackedObject)root).Tracker.Undo();

            Assert.AreEqual(0, root.DependentTrackedObject.P1);
            Assert.AreEqual(0, root.DependentTrackedObject.P2);
            Assert.AreEqual(0, root.DependentTrackedObject.P3);
        }
    }

    [TrackedObject]
    public class AggregateTrackedObject
    {
        private SimpleTrackedObject dependentTrackedObject;

        public int P1 { get; set; }

        public int P2 { get; set; }

        public int P3 { get; set; }

        [NestedTrackedObject]
        public SimpleTrackedObject DependentTrackedObject
        {
            get
            {
                return this.dependentTrackedObject;
            }
            set
            {
                this.dependentTrackedObject = value;
            }
        }

        public void ChangeValues(int? p1 = null, int? p2 = null, int? p3 = null)
        {
            this.P1 = p1.HasValue ? p1.Value : this.P1;
            this.P2 = p2.HasValue ? p2.Value : this.P2;
            this.P3 = p3.HasValue ? p3.Value : this.P3;
        }

        public void ChangeValuesWithDependent(int? p1 = null, int? p2 = null, int? p3 = null)
        {
            this.P1 = p1.HasValue ? p1.Value : this.P1;
            this.P2 = p2.HasValue ? p2.Value : this.P2;
            this.P3 = p3.HasValue ? p3.Value : this.P3;

            this.DependentTrackedObject.P1 = p1.HasValue ? p1.Value : this.P1;
            this.DependentTrackedObject.P2 = p2.HasValue ? p2.Value : this.P2;
            this.DependentTrackedObject.P3 = p3.HasValue ? p3.Value : this.P3;
        }
    }
}