using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.Tests.Integration
{
    [TestFixture]
    class OperationTrackingInpcIntegrationTests
    {
        [Test]
        public void SimpleIntegrationTest()
        {
            TestHelpers.DoInpcTest<InpcAggregateTrackedObject>(
                root =>
                    {
                        root.DependentTrackedObject = new InpcSimpleTrackedObject();

                        var to = (ITrackedObject)root;

                        root.ChangeValuesWithDependent( 1, 2, 3 );

                        to.Undo();

                        Assert.AreEqual( 0, root.P1 );
                        Assert.AreEqual( 0, root.P2 );
                        Assert.AreEqual( 0, root.P3 );

                        Assert.AreEqual( 0, root.DependentTrackedObject.P1 );
                        Assert.AreEqual( 0, root.DependentTrackedObject.P2 );
                        Assert.AreEqual( 0, root.DependentTrackedObject.P3 );
                    },
                5,
                "P1",
                "InnerP1" );
        }
    }

    [TrackedObject]
    [NotifyPropertyChanged]
    public class InpcAggregateTrackedObject
    {
        [ChangeTracked]
        private InpcSimpleTrackedObject dependentTrackedObject;

        public int P1 { get; set; }

        public int P2 { get; set; }

        public int P3 { get; set; }

        public InpcSimpleTrackedObject DependentTrackedObject
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

        public int InnerP1
        {
            get
            {
                return this.dependentTrackedObject.P1;
            }
        }

        public int InnerP2
        {
            get
            {
                return this.dependentTrackedObject.P2;
            }
        }

        public int InnerP3
        {
            get
            {
                return this.dependentTrackedObject.P3;
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

    [TrackedObject]
    [NotifyPropertyChanged]
    public class InpcSimpleTrackedObject
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
