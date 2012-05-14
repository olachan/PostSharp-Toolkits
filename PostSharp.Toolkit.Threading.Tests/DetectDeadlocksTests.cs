using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using PostSharp.Toolkit.Threading.Deadlock;

[assembly: DetectDeadlocks(AttributeTargetAssemblies = "mscorlib", AttributeTargetTypes = "System.Threading.*")]

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class DetectDeadlocksTests
    {


        [Test]
        [ExpectedException(typeof(DeadlockException))]
        public void Test()
        {
            var lock1 = new Lock1();
            var lock2 = new Lock2();
            var barrier = new Barrier(3);
            Task t1 = new Task(() =>
                {
                    lock (lock1)
                    {
                        barrier.SignalAndWait();
                        lock (lock2)
                        {
                            Thread.Sleep(100);
                        }
                    }
                });

            Task t2 = new Task(() =>
            {
                lock (lock2)
                {
                    barrier.SignalAndWait();
                    lock (lock1)
                    {
                        Thread.Sleep(100);
                    }
                }
            });   

            t1.Start();
            t2.Start();

            barrier.SignalAndWait();
            Thread.Sleep(1000);

            DeadlockMonitor.DetectDeadlocks();

            t1.Wait();
            t2.Wait();
        }


    }

    public class Lock1 : object 
    {}

    public class Lock2 : object
    { }
}
