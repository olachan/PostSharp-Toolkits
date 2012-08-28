using System;
using System.Collections.Generic;
using PostSharp.Toolkit.Domain.Tools;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class OperationCollection : IOperationCollection
    {
        //private int currentItemIndex = 0;
        private readonly Stack<IOperation> operations;

        private readonly Dictionary<string, int> namedRestorePoints;

        public OperationCollection()
        {
            this.namedRestorePoints = new Dictionary<string, int>();
            this.operations = new Stack<IOperation>();
        }

        private OperationCollection(Stack<IOperation> operations, Dictionary<string, int> namedRestorePoints)
        {
            this.namedRestorePoints = namedRestorePoints;
            this.operations = operations;
        }

        public IOperationCollection Clone()
        {
            return new OperationCollection(new Stack<IOperation>(this.operations.Reverse()), this.namedRestorePoints.ToDictionary(k => k.Key, v => v.Value));
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

            if (operation.IsRestorePoint())
            {
                this.AddNamedRestorePoint(operation);
            }
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

            this.DecreaseRestorePointCount(operation.Name);

            return operation; //this.operations.Pop();
        }


        private void DecreaseRestorePointCount(string name)
        {
            this.namedRestorePoints[name] -= 1;

            if (this.namedRestorePoints[name] == 0)
            {
                this.namedRestorePoints.Remove(name);
            }
        }

        public void AddNamedRestorePoint(string name)
        {
            this.Push(new RestorePoint(name));
        }

        private void AddNamedRestorePoint(IOperation restorePoint)
        {
            int namedRestorePointCount;

            if (!this.namedRestorePoints.TryGetValue(restorePoint.Name, out namedRestorePointCount))
            {
                namedRestorePointCount = 0;
            }

            this.namedRestorePoints.AddOrUpdate(restorePoint.Name, namedRestorePointCount + 1);
        }

        public Stack<IOperation> GetOperationsToRestorePoint(string name)
        {
            if (!this.namedRestorePoints.ContainsKey(name))
            {
                throw new ArgumentException(string.Format("No restore point named {0}", name));
            }

            this.DecreaseRestorePointCount(name);

            List<IOperation> restoreOperations = new List<IOperation>();

            IOperation restorePoint = null;

            // TODO performance optimization
            while (restorePoint == null || restorePoint.Name != name)
            {
                restorePoint = this.operations.Pop();
                restoreOperations.Add(restorePoint);
            }

            restoreOperations.Reverse();

            return new Stack<IOperation>(restoreOperations);
        }

        public void Clear()
        {
            this.operations.Clear();
            //this.namedRestorePoints.Clear();
            //this.currentItemIndex = 0;
        }

        
    }

}