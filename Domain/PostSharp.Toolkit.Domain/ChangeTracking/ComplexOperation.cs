using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class ComplexOperation : Operation
    {
        private readonly List<SubOperation> subOperations;

        public ComplexOperation( string name )
            : this(new List<SubOperation>(), name)
        {
        }

        public ComplexOperation(List<SubOperation> subOperations, string name)
        {
            this.Name = name;
            this.subOperations = subOperations;
        }

        public void AddOperation(SubOperation change)
        {
            this.subOperations.Add(change);
        }

        public int OperationCount
        {
            get
            {
                return this.subOperations.Count;
            }
        }

        protected internal override void Undo()
        {
            for (int i = this.subOperations.Count - 1; i >= 0; i--)
            {
                this.subOperations[i].Undo();
            }
        }

        protected internal override void Redo()
        {
            foreach (SubOperation operation in this.subOperations)
            {
                operation.Redo();
            }
        }
    }
}