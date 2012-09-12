using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal abstract class Operation : SubOperation
    {
        public string Name { get; protected set; }
    }

    internal abstract class SubOperation
    {
        protected internal abstract void Undo();
        protected internal abstract void Redo();
    }
}