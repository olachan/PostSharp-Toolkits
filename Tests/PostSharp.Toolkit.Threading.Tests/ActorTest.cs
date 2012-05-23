using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PostSharp.Toolkit.Threading.Dispatching;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class ActorTest
    {
        [Test]
        public void Test()
        {
            ActorClass actorClass = new ActorClass();
            Task[] tasks = new Task[10];

            for ( int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new Task( actorClass.Foo );
                tasks[i].Start();
            }

            actorClass.CountdownEvent.Wait();

            Assert.AreEqual( 10,actorClass.Count );
        }
    }

    class ActorClass : Actor
    {
        public CountdownEvent CountdownEvent = new CountdownEvent(10);
            
           
        public int Count;
        public void Foo()
        {
            if ( !Monitor.TryEnter( this ))
            {
                throw new ThreadingException();
            }

            this.Count++;
            
            Thread.Sleep( 100 );
            CountdownEvent.Signal();
            Monitor.Exit( this );

        }
        
    }
}
