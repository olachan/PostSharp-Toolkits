#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public class HistoryTracker : Tracker
    {
        public HistoryTracker Track(ITrackedObject target)
        {
            ((ObjectTracker)target.Tracker).AssociateWithParent(this);
            return this;
        }

        internal override void AddUndoOperationToParentTracker( List<IOperation> operations, OperationCollection undoOperations, OperationCollection redoOperations )
        {   
        }
    }
}