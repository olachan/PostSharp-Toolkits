﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using PostSharp.Toolkit.Domain.OperationTracking;
using PostSharp.Toolkit.Threading;

namespace PostSharp.Toolkit.Domain.Tests.OperationTracking
{
    [TestFixture]
    public class MultiThreadingTests
    {
        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
        public void SimpleMultiThreadTestTrackerTest()
        {
            GlobalTracker globalTracker = new GlobalTracker();
            SlowSimpleTrackedObject sto1 = new SlowSimpleTrackedObject();
            SlowSimpleTrackedObject sto2 = new SlowSimpleTrackedObject();

            globalTracker
                .Track( (ITrackedObject)sto1 )
                .Track( (ITrackedObject)sto2 );

            globalTracker.AddOperation(new DelegateOperation<SlowSimpleTrackedObject>(sto1, o => {Thread.Sleep(100);
                                                                                                  Thread.Yield();
                                                                                                  Thread.Sleep( 100 );}, 
                                                                                            o => {Thread.Sleep(100);
                                                                                                  Thread.Yield();
                                                                                                  Thread.Sleep( 100 );}));

            Task.Factory.StartNew( () =>
                { 
                    globalTracker.Undo();
                    globalTracker.Undo();
                    globalTracker.Undo();
                });

            sto1.ChangeValues(1, 2, 3);

            sto2.ChangeValues( 2,3,4 );
        }
    }

    [TrackedObject]
    public class SlowSimpleTrackedObject : ITrackable
    {
        private int p1;

        private int p2;

        private int p3;

        public int P1
        {
            get
            {
                return this.p1;
            }
            set
            {
                this.p1 = value;
                Thread.Sleep(50);
            }
        }

        public int P2
        {
            get
            {
                return this.p2;
            }
            set
            {
                this.p2 = value;
                Thread.Sleep(50);
            }
        }

        public int P3
        {
            get
            {
                return this.p3;
            }
            set
            {
                this.p3 = value;
                Thread.Sleep(50);
            }
        }

        public void ChangeValues(int? p1 = null, int? p2 = null, int? p3 = null)
        {
            this.P1 = p1.HasValue ? p1.Value : this.P1;
            this.P2 = p2.HasValue ? p2.Value : this.P2;
            this.P3 = p3.HasValue ? p3.Value : this.P3;
        }
    }
}
