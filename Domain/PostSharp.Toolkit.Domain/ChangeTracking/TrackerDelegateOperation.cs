using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class TrackerDelegateOperation : DelegateOperation
    {
        public Tracker Tracker { get; private set; }

        //public TrackerDelegateOperation(Tracker tracker, Action undoAction, Action redoAction)
        //    : base(undoAction, redoAction)
        //{
        //    this.Tracker = tracker;
        //}

        public TrackerDelegateOperation(Tracker tracker, Action undoAction, Action redoAction, string name)
            : base(undoAction, redoAction, name)
        {
            this.Tracker = tracker;
        }
    }
}