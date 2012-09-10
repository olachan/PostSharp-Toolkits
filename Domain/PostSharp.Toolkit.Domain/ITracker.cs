using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain
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

        bool IsTracking { get; }
    }
}