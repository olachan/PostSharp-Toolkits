using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    /// <summary>
    /// Represents operation created by ObjectTracker and representing an operation performed by it
    /// (undo, redo, redo to snapshot etc.)
    /// </summary>
    //TODO: (KW) Debug usages, make sure it works
    internal class ObjectTrackerOperation : IOperation
    {
        protected readonly OperationCollection UndoOperations;

        protected readonly OperationCollection RedoOperations;

        protected readonly List<IOperation> CurrentOperations;

        private ObjectTracker target;

        public ObjectTrackerOperation(ObjectTracker target, OperationCollection undoOperations, OperationCollection redoOperations, List<IOperation> currentOperations)
        {
            this.target = target;
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
            this.CurrentOperations = currentOperations;
        }

        public string Name { get; private set; }

        public void Undo()
        {
            ObjectTracker sot = this.target as ObjectTracker;

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
            ObjectTracker sot = this.target as ObjectTracker;
            using (sot.StartDisabledTrackingScope())
            {
                //TODO: Optimize
                this.CurrentOperations.Reverse();
                foreach ( IOperation currentOperation in this.CurrentOperations )
                {
                    currentOperation.Redo();
                }
                this.CurrentOperations.Reverse();

                sot.SetOperationCollections( this.RedoOperations, this.UndoOperations );
            }
        }
    }
}