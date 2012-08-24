using System;
using System.Threading.Tasks;

using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    public class ThreadingBaseTestFixture
    {
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            // swallow all unobserved exceptions
            TaskScheduler.UnobservedTaskException += ( sender, args ) => args.SetObserved();
        }

        [TearDown]
        public void TearDown()
        {
            // wait for any pending exceptions from background tasks
            try
            {
                GC.Collect(GC.MaxGeneration);
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }

        [SetUp]
        public void SetUp()
        {
            try
            {
                GC.Collect(GC.MaxGeneration);
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }
    }
}