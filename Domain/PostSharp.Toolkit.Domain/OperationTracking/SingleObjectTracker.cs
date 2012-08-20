using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public sealed class SingleObjectTracker : Tracker
    {
        private readonly WeakReference target;

        public SingleObjectTracker(ITrackable target)
        {
            this.target = new WeakReference( target );
        }

        public void AddObjectSnapshot(string name = null)
        {
            ITrackable trackable = this.target.Target as ITrackable;

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
            return new TrackerSnapshot(this);
        }
    }
}