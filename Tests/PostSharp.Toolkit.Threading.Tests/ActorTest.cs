#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class ActorTest : ThreadingBaseTestFixture
    {
        [Test]
        public void Test()
        {
            ActorClass actorClass = new ActorClass();
            Task[] tasks = new Task[10];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new Task(actorClass.Foo);
                tasks[i].Start();
            }

            Assert.True(actorClass.CountdownEvent.Wait(10000));

            Assert.AreEqual(10, actorClass.Count);
        }

        [Test, RequiresMTA]
        public void TestFast()
        {
            const int n = 1000000;
            ActorClass[] actors = new ActorClass[Math.Max(Environment.ProcessorCount - 1, 1)];


            for (int i = 0; i < actors.Length; i++)
            {
                actors[i] = new ActorClass();
            }


            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < actors.Length; j++)
                {
                    actors[j].Fast();
                }
            }


            ManualResetEvent[] readyHandles = new ManualResetEvent[actors.Length];
            for (int i = 0; i < actors.Length; i++)
            {
                actors[i].Set(readyHandles[i] = new ManualResetEvent(false));
            }

            if (!WaitHandle.WaitAll(readyHandles, 20000))
                throw new TimeoutException();
        }
    }

    internal class ActorClass : Actor
    {
        public CountdownEvent CountdownEvent
        {
            [ThreadSafe]
            get;
            [ThreadSafe]
            set;
        }

        public int Count
        {
            [ThreadSafe]
            get;
            [ThreadSafe]
            set;
        }

        public ActorClass()
        {
            this.CountdownEvent = new CountdownEvent(10);
        }

        public void Foo()
        {
            if (!Monitor.TryEnter(this))
            {
                throw new ThreadingException();
            }

            this.Count++;

            Thread.Sleep(100);
            this.CountdownEvent.Signal();
            Monitor.Exit(this);
        }

        public void Fast()
        {
            this.Count++;
        }

        public void Set(ManualResetEvent waitHandle)
        {
            waitHandle.Set();
        }

        [ThreadSafe]
        public override string ToString()
        {
            return string.Format("Actor Count={0}", this.Count);
        }
    }
}