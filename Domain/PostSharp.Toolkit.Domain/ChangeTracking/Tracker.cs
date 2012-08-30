#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using PostSharp.Toolkit.Threading;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [ThreadUnsafeObject]
    public abstract class Tracker : ITracker, ITrackable
    {
        protected class TrackingDisabledToken : IDisposable
        {
            private readonly Tracker tracker;

            private readonly bool enableTrackingOnDispose;

            public TrackingDisabledToken(Tracker tracker)
            {
                this.tracker = tracker;
                this.enableTrackingOnDispose = !tracker.IsTrackingDisabled;
                tracker.DisableTracking();
            }

            public void Dispose()
            {
                if (enableTrackingOnDispose)
                {
                    tracker.EnableTracking();
                }
            }
        }

        protected IOperationCollection UndoOperations;

        protected IOperationCollection RedoOperations;

        public ITracker ParentTracker { get; protected set; }

        protected bool IsTrackingDisabled { get; private set; }

        protected Tracker()
        {
            this.UndoOperations = new OperationCollection();
            this.RedoOperations = new OperationCollection();
            this.IsTrackingDisabled = false;
        }

        public virtual void AddOperation(IOperation operation, bool addToParent = true)
        {
            if (addToParent && this.ParentTracker != null)
            {
                this.ParentTracker.AddOperation(new TargetedDelegateOperation<Tracker>(this, t => t.Undo(false), t => t.Redo(false)));
            }

            this.UndoOperations.Push(operation);
            this.RedoOperations.Clear();
        }

        public virtual void AddNamedRestorePoint(string name)
        {
            // TODO what if there is an opened chunk?
            this.UndoOperations.AddNamedRestorePoint(name);
        }

        public virtual void Undo(bool addToParent = true)
        {
            using (this.StartDisabledTrackingScope())
            {
                IOperationCollection undoOperations = null;
                IOperationCollection redoOperations = null;

                if ( addToParent )
                {
                    undoOperations = this.UndoOperations.Clone();
                    redoOperations = this.RedoOperations.Clone();
                }

                var operation = this.UndoOperations.Pop();
                if ( operation != null )
                {
                    if ( addToParent )
                    {
                        this.AddUndoOperationToParentTracker( operation, undoOperations, redoOperations );
                    }

                    operation.Undo();
                    this.RedoOperations.Push( operation );

                    if ( operation.IsRestorePoint() )
                    {
                        this.Undo( addToParent );
                    }
                }
            }
        }

        public virtual void Redo(bool addToParent = true)
        {
            using (this.StartDisabledTrackingScope())
            {
                IOperationCollection undoOperations = null;
                IOperationCollection redoOperations = null;

                if ( addToParent )
                {
                    undoOperations = this.UndoOperations.Clone();
                    redoOperations = this.RedoOperations.Clone();
                }

                var operation = this.RedoOperations.Pop();
                if ( operation != null )
                {
                    if ( addToParent )
                    {
                        this.AddUndoOperationToParentTracker( operation, undoOperations, redoOperations );
                    }

                    operation.Redo();
                    this.UndoOperations.Push( operation );

                    if ( operation.IsRestorePoint() )
                    {
                        this.Redo( addToParent );
                    }
                }
            }
        }

        public virtual void RestoreNamedRestorePoint(string name)
        {
            using (this.StartDisabledTrackingScope())
            {
                IOperationCollection undoOperations = this.UndoOperations.Clone();
                IOperationCollection redoOperations = this.RedoOperations.Clone();

                Stack<IOperation> snapshotsToResore = this.UndoOperations.GetOperationsToRestorePoint( name );

                var snapshotsForParent = snapshotsToResore.ToList();
                snapshotsForParent.Reverse();

                this.AddUndoOperationToParentTracker( snapshotsForParent, undoOperations, redoOperations );

                while ( snapshotsToResore.Count > 0 )
                {
                    // TODO consider optimization
                    IOperation operation = snapshotsToResore.Pop();
                    operation.Undo();
                    this.RedoOperations.Push( operation );
                }
            }
        }

        public IDisposable StartDisabledTrackingScope()
        {
            return new TrackingDisabledToken( this );
        }

        protected void DisableTracking()
        {
            this.IsTrackingDisabled = true;
        }

        protected void EnableTracking()
        {
            this.IsTrackingDisabled = false;
        }

        protected abstract void AddUndoOperationToParentTracker(List<IOperation> operations, IOperationCollection undoOperations, IOperationCollection redoOperations);

        protected virtual void AddUndoOperationToParentTracker(IOperation operation, IOperationCollection undoOperations, IOperationCollection redoOperations)
        {
            this.AddUndoOperationToParentTracker(new List<IOperation>() { operation }, undoOperations, redoOperations);
        }

    }
}