using System;
using System.Collections.Generic;
using PostSharp.Toolkit.Domain.Tools;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class OperationCollection
    {
        //private int currentItemIndex = 0;
        private readonly Stack<IOperation> operations;

        public OperationCollection()
        {
            this.operations = new Stack<IOperation>();
        }

        private OperationCollection(Stack<IOperation> operations)
        {
            this.operations = operations;
        }

        public OperationCollection Clone()
        {
            return new OperationCollection(new Stack<IOperation>(this.operations.Reverse()));
        }

        public int Count
        {
            get
            {
                return this.operations.Count;
            }
        }

        public void Push(IOperation operation)
        {
            if (operation == null)
            {
                return;
            }

            //this.currentItemIndex++;
            this.operations.Push(operation);
        }

        public IOperation Pop()
        {
            if (this.operations.Count == 0)
            {
                return null;
            }

            IOperation operation = this.operations.Pop();

            if (!operation.IsRestorePoint())
            {
                return operation;
            }

            return operation; //this.operations.Pop();
        }


        public RestorePointToken AddRestorePoint(string name = null)
        {
            var restorePoint = new RestorePoint(name);
            this.Push(restorePoint);
            return restorePoint.Token;
        }

        public Stack<IOperation> GetOperationsToRestorePoint(string name)
        {
            return GetOperationsToRestorePoint(o => o.Name == name);
        }

        public Stack<IOperation> GetOperationsToRestorePoint(RestorePointToken token)
        {
            return GetOperationsToRestorePoint(o => (o is RestorePoint) && ReferenceEquals(((RestorePoint)o).Token, token));
        }

        private Stack<IOperation> GetOperationsToRestorePoint(Predicate<IOperation> predicate)
        {
            List<IOperation> restoreOperations = new List<IOperation>();

            IOperation restorePoint = null;

            // TODO performance optimization
            while (operations.Count > 0 && (restorePoint == null || !predicate(restorePoint)))
            {
                restorePoint = this.operations.Pop();
                restoreOperations.Add(restorePoint);
            }

            if (!predicate(restorePoint))
            {
                throw new ArgumentException("Restore point not found");
            }

            restoreOperations.Reverse();

            return new Stack<IOperation>(restoreOperations);
        }

        public bool ContainsReferenceTo(Tracker tracker)
        {
            return operations.OfType<TrackerDelegateOperation>().Any(o => ReferenceEquals(tracker, o.Tracker)) || 
                   operations.OfType<ObjectTrackerOperation>().Any(o => ReferenceEquals(tracker, o.Tracker));
        }

        public void Clear()
        {
            this.operations.Clear();
            //this.namedRestorePoints.Clear();
            //this.currentItemIndex = 0;
        }
    }
}