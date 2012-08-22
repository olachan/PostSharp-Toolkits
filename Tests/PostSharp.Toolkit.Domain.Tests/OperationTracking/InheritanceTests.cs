﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.
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
    public class InheritanceTests
    {
        [Test]
        public void SimpleInheritanceTest()
        {
            SimpleTrackedObjectDerived to = new SimpleTrackedObjectDerived();
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
    }

    [TrackedObject]
    public class SimpleTrackedObjectBase
    {
        public int P1 { get; set; }

        public int P2 { get; set; }
    }

    public class SimpleTrackedObjectDerived : SimpleTrackedObjectBase
    {
        public int P3 { get; set; }

        public void ChangeValues(int? p1 = null, int? p2 = null, int? p3 = null)
        {
            this.P1 = p1.HasValue ? p1.Value : this.P1;
            this.P2 = p2.HasValue ? p2.Value : this.P2;
            this.P3 = p3.HasValue ? p3.Value : this.P3;
        }
    }
}