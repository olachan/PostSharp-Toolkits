using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface IOperationCollection
    {
        void Push(IOperation operation);

        IOperation Pop();

        Stack<IOperation> GetOperationsToRestorePoint(string name);

        void Clear();

        void AddNamedRestorePoint(string name);

        IOperationCollection Clone();

        //IOperation Pop(ITrackable target);
        int Count { get; }
    }
}