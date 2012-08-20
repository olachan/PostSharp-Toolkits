namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public sealed class SingleObjectTracker : Tracker
    {
        private readonly WeakReference<ITrackable> target;

        public SingleObjectTracker(ITrackable target)
        {
            this.target = new WeakReference<ITrackable>( target );
        }

        public void SetParentTracker(Tracker tracker)
        {
            this.ParentTracker = tracker;
        }

        public void AddObjectSnapshot(string name = null)
        {
            ITrackable trackable = this.target.Target;

            if (trackable!= null)
            {
                ISnapshot snapshot = trackable.TakeSnapshot();
                if (name != null)
                {
                    snapshot.ConvertToNamedRestorePoint( name );
                }

                this.AddSnapshot(snapshot);
            }
        }

        protected override ISnapshot TakeSnapshot()
        {
            ITrackable trackable = this.target.Target;
            if (trackable!= null)
            {
                return new SingleObjectTrackerSnapshot( 
                    this, 
                    this.UndoSnapshots.Clone(), 
                    this.RedoSnapshots.Clone(), 
                    trackable.TakeSnapshot() );
            }

            return null;
        }

        internal void SetSnapshotCollections(ISnapshotCollection undoSnapshots, ISnapshotCollection redoSnapshots)
        {
            this.UndoSnapshots = undoSnapshots;
            this.RedoSnapshots = redoSnapshots;
        }
    }
}