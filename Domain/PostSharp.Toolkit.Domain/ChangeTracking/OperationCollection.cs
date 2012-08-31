using System;
using System.Collections.Generic;
using PostSharp.Toolkit.Domain.Tools;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class OperationCollection
    {
        private readonly LinkedList<IOperation> operations;

        private int maximalOperationsCount;

        public OperationCollection()
        {
            this.operations = new LinkedList<IOperation>();
            this.maximalOperationsCount = int.MaxValue;
        }

        private OperationCollection(LinkedList<IOperation> operations)
        {
            this.operations = operations;
            this.maximalOperationsCount = int.MaxValue;
        }

        public OperationCollection Clone()
        {
            return new OperationCollection(new LinkedList<IOperation>(this.operations));
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

            this.operations.AddLast(operation);

            this.Trim();
        }

        private void Trim()
        {
            while ( this.operations.Count > this.MaximalOperationsCount )
            {
                this.operations.RemoveFirst();
            }
        }

        public IOperation Pop()
        {
            if (this.operations.Count == 0)
            {
                return null;
            }

            IOperation operation = this.operations.Last.Value;
            this.operations.RemoveLast();

            if (!operation.IsRestorePoint())
            {
                return operation;
            }

            return operation; 
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
                restorePoint = this.operations.Last.Value;
                this.operations.RemoveLast();
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
        }

        public int MaximalOperationsCount
        {
            get
            {
                return this.maximalOperationsCount;
            }
            set
            {
                this.maximalOperationsCount = value;
                this.Trim();
            }
        }
    }
}