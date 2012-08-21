using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public class BatchOperation : IOperation
    {
        private readonly List<IOperation> snapshots;

        public BatchOperation()
            : this(new List<IOperation>())
        {
        }

        public BatchOperation(List<IOperation> snapshots)
        {
            this.snapshots = snapshots;
        }

        public void AddOperation(IOperation operation)
        {
            this.snapshots.Add( operation );
        }

        public void Undo()
        {
            foreach ( IOperation snapshot in snapshots )
            {
                snapshot.Undo();
            }
        }

        public void Redo()
        {
            //TODO optimize
            snapshots.Reverse();
            foreach ( IOperation snapshot in snapshots )
            {
                snapshot.Redo();
            }
            snapshots.Reverse();
        }

        public bool IsNamedRestorePoint { get; private set; }

        public string Name { get; private set; }

        public void ConvertToNamedRestorePoint( string name )
        {
            this.IsNamedRestorePoint = true;
            this.Name = name;
        }
    }
}