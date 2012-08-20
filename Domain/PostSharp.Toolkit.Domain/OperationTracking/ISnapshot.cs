namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface ISnapshot
    {
        ISnapshot Restore();
        bool IsNamedRestorePoint { get; }
        string Name { get; }
        void ConvertToNamedRestorePoint( string name );
    }
}