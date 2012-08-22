﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public class GlobalTracker : Tracker
    {
        public GlobalTracker Track(ITrackedObject target)
        {
            target.Tracker.ParentTracker = this;
            return this;
        }

        protected override void AddUndoOperationToParentTracker( List<IOperation> snapshots, IOperationCollection undoOperations, IOperationCollection redoOperations )
        {   
        }
    }
}