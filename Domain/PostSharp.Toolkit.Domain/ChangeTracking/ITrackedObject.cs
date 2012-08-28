namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public interface ITrackedObject : ITrackable
    {
        IObjectTracker Tracker { get; } // TODO make set internal

        [DoNotMakeAutomaticOperation]
        void SetTracker(IObjectTracker tracker);

        int OperationCount { get; }

        [DoNotMakeAutomaticOperation]
        void Undo();

        [DoNotMakeAutomaticOperation]
        void Redo();

        [DoNotMakeAutomaticOperation]
        void AddNamedRestorePoint(string name);

        [DoNotMakeAutomaticOperation]
        void RestoreNamedRestorePoint( string name );
    }
}