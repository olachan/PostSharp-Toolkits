#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Linq;

using NUnit.Framework;

using PostSharp.Toolkit.Domain.OperationTracking;

namespace PostSharp.Toolkit.Domain.Tests.OperationTracking
{
    [TestFixture]
    public class TrackedCollectionTests
    {
        [Test]
        [Ignore]
        public void SimpleTrackedCollectionWithIntsAddTest()
        {
            TrackedCollection<int> tc = new TrackedCollection<int>();

            tc.Add(0);
            tc.Add(1);
            tc.Add(2);
            tc.Add(3);
            tc.Add(4);
            tc.Add(5);

            tc.AddNamedRestorePoint("After 5");

            tc.Add(6);
            tc.Add(7);
            tc.Add(8);
            tc.Add(9);
            tc.Add(10);

            tc.RestoreNamedRestorePoint("After 5");

            Assert.AreEqual( 5, tc.Last() );

            tc.Redo();

            Assert.AreEqual(6, tc.Last());

            tc.Undo();

            Assert.AreEqual(5, tc.Last());

            tc.Undo();

            Assert.AreEqual(4, tc.Last());
        }
    }
}