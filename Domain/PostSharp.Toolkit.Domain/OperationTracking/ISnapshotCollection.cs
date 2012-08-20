using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface ISnapshotCollection
    {
        void Push(ISnapshot snapshot);

        ISnapshot Pop();

        Stack<ISnapshot> GetSnapshotsToRestorePoint(string name);

        void Clear();

        void AddNamedRestorePoint(string name);
    }
}