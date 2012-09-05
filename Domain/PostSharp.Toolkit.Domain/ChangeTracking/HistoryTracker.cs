﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.
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
        private readonly List<AggregateTracker> childTrackers;

        public HistoryTracker()
        {
            childTrackers = new List<AggregateTracker>();
        }

        public HistoryTracker Track(object target)
        {
            var trackedObject = ObjectTracker.CheckObject(target);

            //TODO: What if target is already associated with other HistoryTracker?
            //TODO: What if target already has some history?

            //TODO: What if target is not tracked at the moment? We should start tracking

            ((AggregateTracker)trackedObject.Tracker).AssociateWithParent(this);

            childTrackers.Add((AggregateTracker)trackedObject.Tracker);

            return this;
        }

        protected override bool AddOperationEnabledCheck(bool throwException = true )
        {
            return this.IsTrackingEnabled;
        }

        protected override bool AddRestorePointEnabledCheck(bool throwException = true )
        {
            if (!this.IsTrackingEnabled)
            {
                if (throwException)
                {
                    throw new InvalidOperationException("Can not add restore point to disabled tracker");
                }

                return false;
            }

            return true;
        }

        protected override bool UndoRedoOperationEnabledCheck(bool throwException = true )
        {
            if (!this.IsTrackingEnabled)
            {
                if (throwException)
                {
                    throw new InvalidOperationException("Can not perform operations on disabled tracker");
                }

                return false;
            }

            return true;
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (AggregateTracker aggregateTracker in childTrackers)
            {
                aggregateTracker.AssociateWithParent(null);
            }
        }

        public override void StopTracking()
        {
            base.StopTracking();

            foreach (AggregateTracker aggregateTracker in childTrackers)
            {
                aggregateTracker.StopTracking();
            }
        }

        internal override void AddUndoOperationToParentTracker(List<IOperation> operations, OperationCollection undoOperations, OperationCollection redoOperations, string name)
        {
        }

        public IEnumerable<IOperationInfo> UndoOperations
        {
            get
            {
                return this.UndoOperationCollection.OperationInfos;
            }
        }

        public IEnumerable<IOperationInfo> RedoOperations
        {
            get
            {
                return this.RedoOperationCollection.OperationInfos;
            }
        }

        public void RedoTo(IOperationInfo operationInfo)
        {
            this.RedoToOperation((IOperation)operationInfo);
        }

        public void UndoTo(IOperationInfo operationInfo)
        {
            this.UndoToOperation((IOperation)operationInfo);
        }
    }
}