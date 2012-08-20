#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public abstract class Tracker : ITrackable
    {
        protected ISnapshotCollection UndoSnapshots;

        protected ISnapshotCollection RedoSnapshots;

        protected Tracker ParentTracker;

        protected Tracker()
        {
            this.UndoSnapshots = new SnapshotCollection();
            this.RedoSnapshots = new SnapshotCollection();
        }

        public virtual void AddSnapshot(ISnapshot snapshot)
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
            var snapshot = this.UndoSnapshots.Pop();
            if (snapshot != null)
            {
                this.RedoSnapshots.Push( snapshot.Restore() );

                if (snapshot is SnapshotCollection.EmptyNamedRestorePoint)
                {
                    this.Undo();
                }
            }
        }

        public virtual void Redo()
        {
            // TODO what should happen here? add next snapshopt to parent or delete last(proper) snapshot from parent
            this.AddSnapshotOfThisToParentTracker();

            var snapshot = this.RedoSnapshots.Pop();
            if (snapshot != null)
            {
                this.UndoSnapshots.Push(snapshot.Restore());

                if (snapshot is SnapshotCollection.EmptyNamedRestorePoint)
                {
                    this.Redo();
                }
            }
        }

        public virtual void RestoreNamedRestorePoint(string name)
        {
            // TODO consider batch redo functionality

            this.AddSnapshotOfThisToParentTracker();

            Stack<ISnapshot> snapshotsToResore = this.UndoSnapshots.GetSnapshotsToRestorePoint(name);

            // Stack<ISnapshot> redoBatch = new Stack<ISnapshot>();

            while (snapshotsToResore.Count > 0)
            {
                // TODO consider optimization
                //redoBatch.Push(snapshotsToResore.Pop().Restore());
                this.RedoSnapshots.Push(snapshotsToResore.Pop().Restore());
            }

            //this.RedoSnapshots.Push( new BatchSnapshot( redoBatch ) );
        }

        protected virtual void AddSnapshotOfThisToParentTracker()
        {
            if (this.ParentTracker != null)
            {
                ParentTracker.AddSnapshot(((ITrackable)this).TakeSnapshot());
            }
        }

        protected abstract ISnapshot TakeSnapshot();

        ISnapshot ITrackable.TakeSnapshot()
        {
            return this.TakeSnapshot();
        }
    }
}