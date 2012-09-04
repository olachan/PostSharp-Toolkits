#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public class HistoryTracker : Tracker
    {
        public HistoryTracker Track(object target)
        {
            var trackedObject = ObjectTracker.CheckObject( target );

            //TODO: What if target is already associated with other HistoryTracker?
            //TODO: What if target already has some history?

            //TODO: What if target is not tracked at the moment? We should start tracking

            ((AggregateTracker)trackedObject.Tracker).AssociateWithParent(this);
            return this;
        }

        protected override bool AddOperationEnabledCheck()
        {
            return this.IsTrackingEnabled;
        }

        protected override bool AddRestorePointEnabledCheck()
        {
            if (!this.IsTrackingEnabled)
            {
                throw new InvalidOperationException("Can not add restore point to disabled tracker");
            }

            return true;
        }

        protected override bool UndoRedoOperationEnabledCheck()
        {
            if (!this.IsTrackingEnabled)
            {
                throw new InvalidOperationException("Can not perform operations on disabled tracker");
            }

            return true;
        }

        internal override void AddUndoOperationToParentTracker( List<IOperation> operations, OperationCollection undoOperations, OperationCollection redoOperations )
        {   
        }

        //TODO: Required API:
        //public IList<OperationInfo> UndoOperations *?
        //public IList<OperationInfo> RedoOperations *?
        //public bool CanUndo()
        //public bool CanRedo()
        //public bool RestorePointExists(string restorePoint)
        //public void RedoTo(OperationInfo ) *?
        //public void UndoTo(OperationInfo ) *?
        //public void Clear()
        //public void TrimHistory(count / operationInfo / restorePoint)
        //* - only history tracker (other methods -> all trackers)
    }
}