using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal class ObjectTrackerOperation : Operation
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
            bool prevoiusDisableCollectingData = sot.DisableCollectingData;
            sot.DisableCollectingData = true;

            foreach (IOperation correntOperation in this.CurrentOperations)
            {
                correntOperation.Undo();
            }

            sot.SetOperationCollections(this.UndoOperations, this.RedoOperations);
            sot.DisableCollectingData = prevoiusDisableCollectingData;
        }

        public override void Redo()
        {
            ObjectTracker sot = this.Target as ObjectTracker;
            bool prevoiusDisableCollectingData = sot.DisableCollectingData;
            sot.DisableCollectingData = true;

            this.CurrentOperations.Reverse();
            foreach (IOperation currentOperation in this.CurrentOperations)
            {
                currentOperation.Redo();
            }
            this.CurrentOperations.Reverse();

            sot.SetOperationCollections(this.RedoOperations, this.UndoOperations);
            sot.DisableCollectingData = prevoiusDisableCollectingData;
        }
    }
}