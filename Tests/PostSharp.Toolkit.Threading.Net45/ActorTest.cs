using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PostSharp.Toolkit.Threading;

namespace PostSharp.Toolkit.Threading.Net45
{

    [TestClass]
    public class ActorTest
    {
        public static Thread MainThread;

        [TestMethod]
        public void TestVoidAsyncMethod()
        {
            ManualResetEventSlim doneEvent = new ManualResetEventSlim();

            ActorClass actor = new ActorClass();

            MainThread = Thread.CurrentThread;
            actor.VoidAsyncMethod(doneEvent);

            doneEvent.Wait();

        }

        [TestMethod]
        public void TestTaskAsyncMethod()
        {
            ManualResetEventSlim doneEvent = new ManualResetEventSlim();

            ActorClass actor = new ActorClass();

            MainThread = Thread.CurrentThread;
            actor.TaskAsyncMethod(doneEvent);


            doneEvent.Wait();
        }

        [TestMethod]
        public void TestReturnAsyncMethod()
        {
            ManualResetEventSlim doneEvent = new ManualResetEventSlim();

            ActorClass actor = new ActorClass();

            MainThread = Thread.CurrentThread;
            actor.ReturnAsyncMethod(doneEvent);


            doneEvent.Wait();
        }

        [TestMethod]
        public void TestPing()
        {
             NotAnActor ping = new NotAnActor();
             NotAnActor pong = new NotAnActor();

            Task t = ping.Ping(pong, 10);

            t.Wait();

            Assert.AreEqual(6, ping.PingCount);
            Assert.AreEqual(5, pong.PingCount);
        }

        [TestMethod]
        public void TestPingNotActor()
        {
            NotAnActor ping = new NotAnActor();
            NotAnActor pong = new NotAnActor();

            Task t = ping.Ping(pong, 10);

            t.Wait();

            Assert.AreEqual(6, ping.PingCount);
            Assert.AreEqual(5, pong.PingCount);
        }
    }

    public class NotAnActor
    {
        [ThreadSafe]
        public int PingCount;

        public async Task Ping(NotAnActor peer, int counter)
        {
            await Task.Yield();

            this.PingCount++;

            if (counter > 0)
            {
                await peer.Ping(this, counter - 1);
            }
        }
    }


    public class ActorClass : Actor
    {

        [ThreadSafe]
        public int PingCount;

        public async Task Ping(ActorClass peer, int counter )
        {
            this.PingCount++;

            if (counter > 0)
            {
                await peer.Ping(this, counter - 1);
            }
        }

        public async void VoidAsyncMethod(ManualResetEventSlim doneEvent)
        {
            Assert.AreNotEqual(ActorTest.MainThread, Thread.CurrentThread);

            int i = 1;
            Assert.IsTrue(this.Dispatcher.CheckAccess());
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 1);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 2);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 3);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 4);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 5);
            doneEvent.Set();

        }

        public async Task TaskAsyncMethod(ManualResetEventSlim doneEvent)
        {
            Assert.AreNotEqual(ActorTest.MainThread, Thread.CurrentThread);

            int i = 1;
            Assert.IsTrue(this.Dispatcher.CheckAccess());
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 1);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 2);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 3);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 4);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 5);
            doneEvent.Set();

        }

        public async Task<int> ReturnAsyncMethod(ManualResetEventSlim doneEvent)
        {
            Assert.AreNotEqual(ActorTest.MainThread, Thread.CurrentThread);

            int i = 1;
            Assert.IsTrue(this.Dispatcher.CheckAccess());
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 1);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 2);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 3);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 4);
            i++;
            await Task.Yield();

            Assert.IsTrue(this.Dispatcher.CheckAccess());
            Assert.AreEqual(i, 5);
            doneEvent.Set();

            return i;
        }
    }
}
