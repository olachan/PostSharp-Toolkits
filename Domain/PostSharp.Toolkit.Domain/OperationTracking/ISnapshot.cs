namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface ISnapshot
    {
        ISnapshot Restore();
        bool IsNamedRestorePoint { get; }
        string Name { get; }

        // ITrackable SnapshotTarget { get; }

        void ConvertToNamedRestorePoint( string name );
    }
}