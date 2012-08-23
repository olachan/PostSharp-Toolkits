using System;

using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    public class ThreadingBaseTestFixture
    {
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