namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface ITrackedObject : ITrackable
    {
        IObjectTracker Tracker { get; }

        [DoNotMakeAutomaticOperation]
        void Undo();

        [DoNotMakeAutomaticOperation]
        void Redo();

        //[DoNotMakeAutomaticSnapshot]
        //void AddObjectSnapshot( string name );

        //[DoNotMakeAutomaticSnapshot]
        //void AddObjectSnapshot();

        [DoNotMakeAutomaticOperation]
        void AddNamedRestorePoint(string name);

        [DoNotMakeAutomaticOperation]
        void RestoreNamedRestorePoint( string name );
    }
}