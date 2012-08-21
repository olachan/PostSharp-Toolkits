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
    public class AggregateTrackerTests
    {
        //TODO: feature not implemented
        [Test]
        //[Ignore]
        public void SimpleGlobalTrackerTest()
        {
            AggregateTrackedObject root = new AggregateTrackedObject();
            
            root.DependentTrackedObject = new SimpleTrackedObject();
            
            var to = (ITrackedObject)root;
            root.ChangeValues(1, 2, 3);

            to.Undo();

            Assert.AreEqual(0, root.P1);
            Assert.AreEqual(0, root.P2);
            Assert.AreEqual(0, root.P3);

            root.DependentTrackedObject.ChangeValues( 1, 2, 3 );

            to.Undo();

            Assert.AreEqual(0, root.P1);
            Assert.AreEqual(0, root.P2);
            Assert.AreEqual(0, root.P3);

            Assert.AreEqual(0, root.DependentTrackedObject.P1);
            Assert.AreEqual(0, root.DependentTrackedObject.P2);
            Assert.AreEqual(0, root.DependentTrackedObject.P3);
        }

        [Test]
        //[Ignore]
        public void AggregateMethodChangingDependentTest()
        {
            AggregateTrackedObject root = new AggregateTrackedObject();

            root.DependentTrackedObject = new SimpleTrackedObject();

            var to = (ITrackedObject)root;

            root.ChangeValuesWithDependent(1, 2, 3);

            to.Undo();

            Assert.AreEqual(0, root.P1);
            Assert.AreEqual(0, root.P2);
            Assert.AreEqual(0, root.P3);

            Assert.AreEqual(0, root.DependentTrackedObject.P1);
            Assert.AreEqual(0, root.DependentTrackedObject.P2);
            Assert.AreEqual(0, root.DependentTrackedObject.P3);
        }
    }

    [TrackedObject]
    public class AggregateTrackedObject
    {
        [TrackedProperty]
        private SimpleTrackedObject dependentTrackedObject;

        public int P1 { get; set; }

        public int P2 { get; set; }

        public int P3 { get; set; }

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