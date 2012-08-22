using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal class SingleObjectTrackerOperation : ObjectTrackerOperation
    {
        public SingleObjectTrackerOperation(SingleObjectTracker target, IOperationCollection undoOperations, IOperationCollection redoOperations, IOperation currentOperation)
            : base(target, undoOperations, redoOperations, new List<IOperation>() { currentOperation })
        {
        }
    }
}