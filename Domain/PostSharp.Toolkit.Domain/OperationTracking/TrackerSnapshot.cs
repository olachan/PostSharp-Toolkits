namespace PostSharp.Toolkit.Domain.OperationTracking
{
    class TrackerSnapshot : Snapshot
    {
        // TODO implement
        public TrackerSnapshot( ITrackable target )
            : base( target )
        {
        }

        public override ISnapshot Restore()
        {
            throw new System.NotImplementedException();
        }
    }
}