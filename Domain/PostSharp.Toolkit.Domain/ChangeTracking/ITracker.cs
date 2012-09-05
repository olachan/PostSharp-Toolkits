namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public interface ITracker
    {
        RestorePointToken AddRestorePoint(string name = null);

        void Undo();

        void Redo();

        void UndoTo(string name);

        void UndoTo(RestorePointToken token);

        void RedoTo(string name);

        void RedoTo(RestorePointToken token);

        void Track();

        void StopTracking();

        bool CanStopTracking();

        int MaximumOperationsCount { get; set; }
    }
}