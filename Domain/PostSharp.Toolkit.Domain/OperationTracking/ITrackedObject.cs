namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface ITrackedObject : ITrackable
    {
        [DoNotTrack]
        void Undo();

        [DoNotTrack]
        void Redo();

        [DoNotTrack]
        void AddObjectSnapshot( string name );

        [DoNotTrack]
        void AddObjectSnapshot();

        [DoNotTrack]
        void AddNamedRestorePoint(string name);

        [DoNotTrack]
        void RestoreNamedRestorePoint( string name );
    }
}