using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public abstract class Operation : SubOperation
    {
        public string Name { get; protected set; }
    }

    public abstract class SubOperation
    {
        protected internal abstract void Undo();
        protected internal abstract void Redo();
    }
}