namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface IOperation
    {
        void Undo();
        bool IsNamedRestorePoint { get; }
        string Name { get; }
        void ConvertToNamedRestorePoint( string name );

        void Redo();
    }
}