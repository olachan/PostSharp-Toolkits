namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public interface ITracker
    {
        void AddOperation(IOperation operation, bool addToParent = true);

        void AddNamedRestorePoint(string name);

        void Undo(bool addToParent = true);

        void Redo(bool addToParent = true);

        void RestoreNamedRestorePoint(string name);
    }
}