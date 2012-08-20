namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal class SingleObjectTrackerSnapshot : Snapshot
    {
        private readonly ISnapshotCollection undoSnapshots;

        private readonly ISnapshotCollection redoSnapshots;

        private readonly ISnapshot currentSnapshot;

        // TODO implement
        public SingleObjectTrackerSnapshot(SingleObjectTracker target, ISnapshotCollection undoSnapshots, ISnapshotCollection redoSnapshots, ISnapshot currentSnapshot)
            : base( target )
        {
            this.undoSnapshots = undoSnapshots;
            this.redoSnapshots = redoSnapshots;
            this.currentSnapshot = currentSnapshot;
        }

        public override ISnapshot Restore()
        {
            SingleObjectTracker sot = Target.Target as SingleObjectTracker;

            if (sot == null)
            {
                return null;
            }

            var currentState = ((ITrackable)sot).TakeSnapshot();

            currentSnapshot.Restore();
            sot.SetSnapshotCollections( this.undoSnapshots, this.redoSnapshots );

            return currentState;
        }
    }
}