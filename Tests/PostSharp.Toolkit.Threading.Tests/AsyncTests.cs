#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using PostSharp.Toolkit.Threading.Dispatching;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class AsyncTests
    {
        [Test]
        public void WhenAsyncCalledInvokesAsynchronously()
        {
            AsyncEntity a = new AsyncEntity();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            a.Async( 100 );
            stopwatch.Stop();
            Assert.Less( stopwatch.ElapsedMilliseconds, 50 );
        }
    }

    public class AsyncEntity
    {
        [BackgroundMethod]
        public void Async( int timespan )
        {
            Thread.Sleep( timespan );
        }
    }
}