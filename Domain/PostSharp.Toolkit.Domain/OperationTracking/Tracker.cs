#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public abstract class Tracker : IOperationTrackable
    {
        // TODO scope should be protected but SnapshotCollection should be internal
        private SnapshotCollection UndoSnapshots;

        private SnapshotCollection RedoSnapshots;

        protected Tracker ParentTracker;

        protected Tracker()
        {
            this.UndoSnapshots = new SnapshotCollection();
            this.RedoSnapshots = new SnapshotCollection();
        }

        public virtual void AddSnapshot(Snapshot snapshot)
        {
            this.UndoSnapshots.Push(snapshot);
            this.RedoSnapshots.Clear();
        }

        public virtual void AddNamedRestorePoint(string name)
        {
            this.UndoSnapshots.AddNamedRestorePoint(name);
        }

        public virtual void Undo()
        {
            this.AddSnapshotOfThisToParentTracker();

            Snapshot undoSnapshot = this.UndoSnapshots.Pop();
            this.RedoSnapshots.Push(undoSnapshot.SnapshotTarget.TakeSnapshot());
            undoSnapshot.Restore();
        }

        public virtual void Redo()
        {
            // TODO what should happen here? add next snapshopt to parent or delete last(proper) snapshot from parent
            this.AddSnapshotOfThisToParentTracker();

            Snapshot redoSnapshot = this.RedoSnapshots.Pop();
            this.UndoSnapshots.Push(redoSnapshot);
            redoSnapshot.Restore();
        }

        public virtual void RestoreNamedRestorePoint(string name)
        {
            // TODO batch redo functionality

            this.AddSnapshotOfThisToParentTracker();

            Stack<Snapshot> snapshotsToResore = this.UndoSnapshots.GetSnapshotsToRestorePoint(name);

            while (snapshotsToResore.Count > 0)
            {
                Snapshot undoSnapshot = snapshotsToResore.Pop();
                // TODO consider optimization
                this.RedoSnapshots.Push(undoSnapshot.SnapshotTarget.TakeSnapshot());
                undoSnapshot.Restore();
            }
        }

        protected virtual void AddSnapshotOfThisToParentTracker()
        {
            if (this.ParentTracker != null)
            {
                ParentTracker.AddSnapshot(((IOperationTrackable)this).TakeSnapshot());
            }
        }

        protected abstract Snapshot TakeSnapshot();

        Snapshot IOperationTrackable.TakeSnapshot()
        {
            return this.TakeSnapshot();
        }
    }
}