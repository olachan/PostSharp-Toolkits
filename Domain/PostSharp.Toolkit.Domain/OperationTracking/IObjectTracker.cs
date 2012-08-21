namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface IObjectTracker : ITrackable
    {
        void SetParentTracker(Tracker tracker); //TODO: internal interface?

        // void AddObjectSnapshot(string name = null);

        void AddNamedRestorePoint(string name);

        void Undo(bool addToParent = true);

        void Redo(bool addToParent = true);

        void RestoreNamedRestorePoint(string name);

        void StartChunk();

        void EndChunk();

        void AddOperationToChunk(IOperation operation);

        bool IsChunkActive { get; }
    }
}