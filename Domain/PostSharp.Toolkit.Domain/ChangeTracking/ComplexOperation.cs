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
            //TODO: optimize
            this.subOperations.Reverse();
            foreach (ISubOperation oper in this.subOperations)
            {
                oper.Undo();
            }
            this.subOperations.Reverse();
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