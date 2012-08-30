namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public interface ITracker
    {
        RestorePointToken AddRestorePoint(string name = null);

        void Undo(bool addToParent = true);

        void Redo(bool addToParent = true);

        void UndoTo(string name);

        void UndoTo(RestorePointToken token);

        void RedoTo(string name);

        void RedoTo(RestorePointToken token);
    }
}