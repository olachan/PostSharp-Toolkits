namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface ITrackedObject : ITrackable
    {
        SingleObjectTracker Tracker { get; }

        [DoNotMakeAutomaticSnapshot]
        void Undo();

        [DoNotMakeAutomaticSnapshot]
        void Redo();

        [DoNotMakeAutomaticSnapshot]
        void AddObjectSnapshot( string name );

        [DoNotMakeAutomaticSnapshot]
        void AddObjectSnapshot();

        [DoNotMakeAutomaticSnapshot]
        void AddNamedRestorePoint(string name);

        [DoNotMakeAutomaticSnapshot]
        void RestoreNamedRestorePoint( string name );
    }
}