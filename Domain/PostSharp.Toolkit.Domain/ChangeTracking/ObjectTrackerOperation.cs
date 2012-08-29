using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    /// <summary>
    /// Represents operation created by ObjectTracker and representing an operation performed by it
    /// (undo, redo, redo to snapshot etc.)
    /// </summary>
    //TODO: (KW) Debug usages, make sure it works
    internal class ObjectTrackerOperation : TargetedOperation
    {
        protected readonly IOperationCollection UndoOperations;

        protected readonly IOperationCollection RedoOperations;

        protected readonly List<IOperation> CurrentOperations;

        public ObjectTrackerOperation(ObjectTracker target, IOperationCollection undoOperations, IOperationCollection redoOperations, List<IOperation> currentOperations)
            : base(target)
        {
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
            this.CurrentOperations = currentOperations;
        }

        public override void Undo()
        {
            ObjectTracker sot = this.Target as ObjectTracker;
            bool previusDisableCollectingData = sot.IsTrackingDisabled; //TODO: Make exception safe! using (tracker.DisableTracking) { ... }?
            sot.IsTrackingDisabled = true;

            foreach (IOperation correntOperation in this.CurrentOperations)
            {
                correntOperation.Undo();
            }

            sot.SetOperationCollections(this.UndoOperations, this.RedoOperations);
            sot.IsTrackingDisabled = previusDisableCollectingData;
        }

        public override void Redo()
        {
            ObjectTracker sot = this.Target as ObjectTracker;
            bool prevoiusDisableCollectingData = sot.IsTrackingDisabled; //TODO: Make exception safe! using (tracker.DisableTracking) { ... }?
            sot.IsTrackingDisabled = true;
            
            //TODO: Optimize
            this.CurrentOperations.Reverse();
            foreach (IOperation currentOperation in this.CurrentOperations)
            {
                currentOperation.Redo();
            }
            this.CurrentOperations.Reverse();

            sot.SetOperationCollections(this.RedoOperations, this.UndoOperations);
            sot.IsTrackingDisabled = prevoiusDisableCollectingData;
        }
    }
}