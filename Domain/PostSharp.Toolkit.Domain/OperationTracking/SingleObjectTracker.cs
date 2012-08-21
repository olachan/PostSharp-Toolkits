namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public sealed class SingleObjectTracker : ObjectTracker
    {
        public SingleObjectTracker(ITrackable target)
            : base( target )
        {
        }
    }
}