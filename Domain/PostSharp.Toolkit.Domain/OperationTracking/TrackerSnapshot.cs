namespace PostSharp.Toolkit.Domain.OperationTracking
{
    class TrackerSnapshot : Snapshot
    {
        // TODO implement
        public TrackerSnapshot( IOperationTrackable target )
            : base( target )
        {
        }

        public override void Restore()
        {
            throw new System.NotImplementedException();
        }
    }
}