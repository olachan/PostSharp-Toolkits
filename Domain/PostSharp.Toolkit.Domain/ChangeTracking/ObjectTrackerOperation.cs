using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    /// <summary>
    /// Represents operation created by AggregateTracker and representing an operation performed by it
    /// (undo, redo, redo to snapshot etc.)
    /// </summary>
    //TODO: (KW) Debug usages, make sure it works
    internal class ObjectTrackerOperation : Operation
    {
        protected readonly OperationCollection UndoOperations;

        protected readonly OperationCollection RedoOperations;

        protected readonly List<Operation> CurrentOperations;

        public AggregateTracker Tracker { get; private set; }

        public ObjectTrackerOperation( AggregateTracker tracker, string operations, OperationCollection undoOperations, OperationCollection redoOperations, List<Operation> currentOperations )
        {
            this.Tracker = tracker;
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
            this.CurrentOperations = currentOperations;
        }

        protected internal override void Undo()
        {
            using (this.Tracker.StartDisabledTrackingScope())
            {
                foreach (Operation correntOperation in this.CurrentOperations)
                {
                    correntOperation.Undo();
                }

                this.Tracker.SetOperationCollections(this.UndoOperations, this.RedoOperations);
            }
        }

        protected internal override void Redo()
        {
            using (this.Tracker.StartDisabledTrackingScope())
            {
                for (int i = this.CurrentOperations.Count - 1; i >= 0; i--)
                {
                    this.CurrentOperations[i].Redo();
                }

                this.Tracker.SetOperationCollections(this.RedoOperations, this.UndoOperations);
            }
        }
    }
}