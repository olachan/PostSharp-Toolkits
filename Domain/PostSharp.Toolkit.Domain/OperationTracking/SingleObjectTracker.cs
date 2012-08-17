using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public sealed class SingleObjectTracker : Tracker
    {
        private readonly WeakReference target;

        public SingleObjectTracker(IOperationTrackable target)
        {
            this.target = new WeakReference( target );
        }

        public void AddObjectSnapshot()
        {
            IOperationTrackable trackable = this.target.Target as IOperationTrackable;

            if (trackable!= null)
            {
                this.AddSnapshot( trackable.TakeSnapshot() );
            }
        }

        public void AddNamedSnapshot(string name)
        {
            this.AddNamedRestorePoint(name);
            this.AddObjectSnapshot();
        }

        protected override Snapshot TakeSnapshot()
        {
            return new TrackerSnapshot(this);
        }

        protected override void RestoreSnapshot(Snapshot snapshot)
        {
            throw new NotImplementedException();
        }
    }
}