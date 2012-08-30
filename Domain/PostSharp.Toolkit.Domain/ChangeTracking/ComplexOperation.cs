using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class ComplexOperation : IOperation
    {
        private readonly List<ISubOperation> subOperations;

        public ComplexOperation()
            : this(new List<ISubOperation>())
        {
        }

        public ComplexOperation(List<ISubOperation> subOperations)
        {
            this.subOperations = subOperations;
        }

        public void AddOperation(ISubOperation change)
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

        public void Undo()
        {
            for (int i = this.subOperations.Count - 1; i >= 0; i--)
            {
                this.subOperations[i].Undo();
            }
        }

        public void Redo()
        {
            foreach (ISubOperation oper in this.subOperations)
            {
                oper.Redo();
            }
        }

        public string Name { get; private set; }

    }
}