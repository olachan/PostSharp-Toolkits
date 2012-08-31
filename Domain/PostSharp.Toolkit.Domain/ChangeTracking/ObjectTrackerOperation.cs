using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    /// <summary>
    /// Represents operation created by AggregateTracker and representing an operation performed by it
    /// (undo, redo, redo to snapshot etc.)
    /// </summary>
    //TODO: (KW) Debug usages, make sure it works
    internal class ObjectTrackerOperation : IOperation
    {
        protected readonly OperationCollection UndoOperations;

        protected readonly OperationCollection RedoOperations;

        protected readonly List<IOperation> CurrentOperations;

        public AggregateTracker Tracker { get; private set; }

        public ObjectTrackerOperation(AggregateTracker tracker, OperationCollection undoOperations, OperationCollection redoOperations, List<IOperation> currentOperations)
        {
            this.Tracker = tracker;
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
            this.CurrentOperations = currentOperations;
        }

        public string Name { get; private set; }

        public void Undo()
        {
            AggregateTracker sot = this.Tracker as AggregateTracker;

            using (sot.StartDisabledTrackingScope())
            {
                foreach (IOperation correntOperation in this.CurrentOperations)
                {
                    correntOperation.Undo();
                }

                sot.SetOperationCollections(this.UndoOperations, this.RedoOperations);
            }
        }

        public void Redo()
        {
            AggregateTracker sot = this.Tracker as AggregateTracker;
            using (sot.StartDisabledTrackingScope())
            {
                for (int i = this.CurrentOperations.Count - 1; i >= 0; i--)
                {
                    this.CurrentOperations[i].Redo();
                }

                sot.SetOperationCollections( this.RedoOperations, this.UndoOperations );
            }
        }
    }
}