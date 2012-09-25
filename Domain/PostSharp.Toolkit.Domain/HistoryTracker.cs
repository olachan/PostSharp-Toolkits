#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain
{
    public class HistoryTracker : Tracker
    {
        private readonly List<AggregateTracker> childTrackers;

        public HistoryTracker()
            : base(true)
        {
            this.childTrackers = new List<AggregateTracker>();
            this.UndoOperationCollection.CollectionChanged += UndoOperationCollectionCollectionChanged;
            this.RedoOperationCollection.CollectionChanged += RedoOperationCollectionCollectionChanged;
        }

        private void RedoOperationCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnRedoOperationsChanged( e );
        }

        private void UndoOperationCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnUndoOperationsChanged( e );
        }

        public HistoryTracker Track(object target)
        {
            var trackedObject = ObjectTracker.CheckObject(target);

            if (((AggregateTracker)trackedObject.Tracker).ParentTracker != null)
            {
                throw new InvalidOperationException("Object is already tracked by another history tracker");
            }

            //TODO: What if target already has some history?

            if (!trackedObject.Tracker.IsTracking)
            {
                trackedObject.Tracker.Track();
            }

            ((AggregateTracker)trackedObject.Tracker).AssociateWithParent(this);

            this.childTrackers.Add((AggregateTracker)trackedObject.Tracker);

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

            foreach (AggregateTracker aggregateTracker in this.childTrackers)
            {
                aggregateTracker.AssociateWithParent(null);
            }
        }

        public override void StopTracking()
        {
            base.StopTracking();

            foreach (AggregateTracker aggregateTracker in this.childTrackers)
            {
                aggregateTracker.StopTracking();
            }
        }

        internal override void AddUndoOperationToParentTracker( string name, List<Operation> operations, OperationCollection undoOperations, OperationCollection redoOperations )
        {
        }

        public IEnumerable<Operation> UndoOperations
        {
            get
            {
                return this.UndoOperationCollection.Operations;
            }
        }

        public IEnumerable<Operation> RedoOperations
        {
            get
            {
                return this.RedoOperationCollection.Operations;
            }
        }

        public void RedoTo(Operation operation)
        {
            this.RedoToOperation(operation);
        }

        public void UndoTo(Operation operation)
        {
            this.UndoToOperation(operation);
        }

        public event NotifyCollectionChangedEventHandler RedoOperationsChanged;

        private void OnRedoOperationsChanged( NotifyCollectionChangedEventArgs e )
        {
            NotifyCollectionChangedEventHandler handler = this.RedoOperationsChanged;
            if ( handler != null )
            {
                handler( this, e );
            }
        }

        public event NotifyCollectionChangedEventHandler UndoOperationsChanged;

        private void OnUndoOperationsChanged( NotifyCollectionChangedEventArgs e )
        {
            NotifyCollectionChangedEventHandler handler = this.UndoOperationsChanged;
            if ( handler != null )
            {
                handler( this, e );
            }
        }
    }
}