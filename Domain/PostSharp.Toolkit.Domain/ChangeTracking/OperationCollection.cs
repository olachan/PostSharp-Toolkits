using System;
using System.Collections.Generic;
using PostSharp.Toolkit.Domain.Tools;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class OperationCollection
    {
        private readonly LinkedList<Operation> operations;

        private int maximumOperationsCount;

        public OperationCollection()
        {
            this.operations = new LinkedList<Operation>();
            this.maximumOperationsCount = int.MaxValue;
        }

        private OperationCollection(LinkedList<Operation> operations)
        {
            this.operations = operations;
            this.maximumOperationsCount = int.MaxValue;
        }

        public OperationCollection Clone()
        {
            return new OperationCollection(new LinkedList<Operation>(this.operations));
        }

        public IEnumerable<Operation> Operations
        {
            get
            {
                return operations;
            }
        }

        public int Count
        {
            get
            {
                return this.operations.Count;
            }
        }

        public void Push(Operation operation)
        {
            if (operation == null)
            {
                return;
            }

            this.operations.AddLast(operation);

            this.Trim();
        }

        public void Trim(int? count = null)
        {
            while (this.operations.Count > (count ?? this.MaximumOperationsCount))
            {
                this.operations.RemoveFirst();
            }
        }

        public void Trim(string restorePoint)
        {
            int? index = this.RestorePointIndex(o => o.Name == restorePoint);

            if (!index.HasValue)
            {
                return;
            }

            for (int i = 0; i < index.Value; i++)
            {
                this.operations.RemoveFirst();
            }
        }

        public void Trim(RestorePointToken restorePoint)
        {
            int? index = this.RestorePointIndex(o => (o is RestorePoint) && ReferenceEquals(((RestorePoint)o).Token, restorePoint));

            if (!index.HasValue)
            {
                return;
            }

            for (int i = 0; i < index.Value; i++)
            {
                this.operations.RemoveFirst();
            }
        }

        public Operation Pop()
        {
            if (this.operations.Count == 0)
            {
                return null;
            }

            Operation operation = this.operations.Last.Value;
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

        public Stack<Operation> GetOperationsToRestorePoint(string name)
        {
            return GetOperationsToRestorePoint(o => o.Name == name);
        }

        public Stack<Operation> GetOperationsToRestorePoint(RestorePointToken token)
        {
            return GetOperationsToRestorePoint(o => (o is RestorePoint) && ReferenceEquals(((RestorePoint)o).Token, token));
        }

        public Stack<Operation> GetOperationsToRestorePoint(Operation operation)
        {
            return GetOperationsToRestorePoint(o => ReferenceEquals(o, operation));
        }

        private Stack<Operation> GetOperationsToRestorePoint(Predicate<Operation> predicate)
        {
            List<Operation> restoreOperations = new List<Operation>();

            Operation restorePoint = null;

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

            return new Stack<Operation>(restoreOperations);
        }

        public bool RestorePointExists(string name)
        {
            return this.RestorePointIndex(o => o.Name == name) != null;
        }

        public bool RestorePointExists(RestorePointToken token)
        {
            return this.RestorePointIndex(o => (o is RestorePoint) && ReferenceEquals(((RestorePoint)o).Token, token)) != null;
        }

        private int? RestorePointIndex(Predicate<Operation> predicate)
        {
            LinkedListNode<Operation> restorePoint = this.operations.Last;

            for (int i = operations.Count; i > 0; i--)
            {
                if (predicate(restorePoint.Value))
                {
                    return i;
                }

                restorePoint = restorePoint.Previous;
            }

            return null;
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

        public int MaximumOperationsCount
        {
            get
            {
                return this.maximumOperationsCount;
            }
            set
            {
                this.maximumOperationsCount = value;
                this.Trim();
            }
        }
    }
}