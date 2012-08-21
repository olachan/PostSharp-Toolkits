#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public abstract class Tracker : ITrackable
    {
        protected IOperationCollection UndoOperations;

        protected IOperationCollection RedoOperations;

        protected Tracker ParentTracker;

        public bool DisableCollectingData { get; set; }

        protected Tracker()
        {
            this.UndoOperations = new OperationCollection();
            this.RedoOperations = new OperationCollection();
            this.DisableCollectingData = false;
        }

        public virtual void AddOperation(IOperation operation, bool addToParent = true)
        {
            if (addToParent && this.ParentTracker != null)
            {
                this.ParentTracker.AddOperation(new DelegateOperation<Tracker>(this, t => t.Undo(false), t => t.Redo(false)));
            }

            this.UndoOperations.Push(operation);
            this.RedoOperations.Clear();
        }

        public virtual void AddNamedRestorePoint(string name)
        {
            this.UndoOperations.AddNamedRestorePoint(name);
        }

        public virtual void Undo(bool addToParent = true)
        {
            this.DisableCollectingData = true;
            
            IOperationCollection undoOperations = this.UndoOperations.Clone();
            IOperationCollection redoOperations = this.RedoOperations.Clone();

            var snapshot = this.UndoOperations.Pop();
            if (snapshot != null)
            {
                if (addToParent)
                {
                    this.AddUndoOperationToParentTracker(snapshot, undoOperations, redoOperations);
                }

                snapshot.Undo();
                this.RedoOperations.Push(snapshot);

                if (snapshot is OperationCollection.EmptyNamedRestorePoint)
                {
                    this.Undo();
                }
            }
            this.DisableCollectingData = false;

        }

        public virtual void Redo(bool addToParent = true)
        {
            this.DisableCollectingData = true;

            // TODO what should happen here? add next snapshopt to parent or delete last(proper) operation from parent

            IOperationCollection undoOperations = this.UndoOperations.Clone();
            IOperationCollection redoOperations = this.RedoOperations.Clone();
            var snapshot = this.RedoOperations.Pop();
            if (snapshot != null)
            {
                if (addToParent)
                {
                    this.AddUndoOperationToParentTracker(snapshot, undoOperations, redoOperations);
                }

                snapshot.Redo();
                this.UndoOperations.Push(snapshot);

                if (snapshot is OperationCollection.EmptyNamedRestorePoint)
                {
                    this.Redo();
                }
            }
            this.DisableCollectingData = false;

        }

        public virtual void RestoreNamedRestorePoint(string name)
        {
            this.DisableCollectingData = true;

            IOperationCollection undoOperations = this.UndoOperations.Clone();
            IOperationCollection redoOperations = this.RedoOperations.Clone();

            Stack<IOperation> snapshotsToResore = this.UndoOperations.GetOperationsToRestorePoint(name);

            var snapshotsForParent = snapshotsToResore.ToList();
            snapshotsForParent.Reverse();

            this.AddUndoOperationToParentTracker(snapshotsForParent, undoOperations, redoOperations);

            // Stack<IOperation> redoBatch = new Stack<IOperation>();

            while (snapshotsToResore.Count > 0)
            {
                // TODO consider optimization
                //redoBatch.Push(snapshotsToResore.Pop().Undo());
                IOperation operation = snapshotsToResore.Pop();


                operation.Undo();
                this.RedoOperations.Push(operation);
            }
            this.DisableCollectingData = false;

            //this.redoOperations.Push( new BatchOperation( redoBatch ) );
        }

        protected abstract void AddUndoOperationToParentTracker(List<IOperation> snapshots, IOperationCollection undoOperations, IOperationCollection redoOperations);

        protected virtual void AddUndoOperationToParentTracker(IOperation operation, IOperationCollection undoOperations, IOperationCollection redoOperations)
        {
            this.AddUndoOperationToParentTracker(new List<IOperation>() { operation }, undoOperations, redoOperations);
        }

    }
}