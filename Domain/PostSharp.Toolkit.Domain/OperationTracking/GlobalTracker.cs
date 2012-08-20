#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public class GlobalTracker : Tracker
    {
        public GlobalTracker Track(ITrackedObject target)
        {
            target.Tracker.SetParentTracker( this );
            return this;
        }

        protected override ISnapshot TakeSnapshot()
        {
            throw new NotSupportedException();
        }
    }
}