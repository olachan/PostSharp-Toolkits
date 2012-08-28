using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public interface IOperation : ISubOperation
    {
        string Name { get; }
    }

    public interface ISubOperation
    {
        void Undo();
        void Redo();
    }
}