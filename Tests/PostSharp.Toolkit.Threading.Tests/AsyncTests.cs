// -----------------------------------------------------------------------
// <copyright file="AsyncTests.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
            
            var a = new AsyncEntity();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            a.Async(100);
            stopwatch.Stop();
            Assert.Less(stopwatch.ElapsedMilliseconds, 50);
        }
    }

    public class AsyncEntity
    {
        [BackgroundMethod]
        public void Async(int timespan)
        {
            Thread.Sleep(timespan);
        }
    }
}
