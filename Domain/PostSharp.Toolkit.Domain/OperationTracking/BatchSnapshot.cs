using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public class BatchSnapshot : ISnapshot
    {
        private readonly Stack<ISnapshot> snapshots;

        public BatchSnapshot( Stack<ISnapshot> snapshots )
        {
            this.snapshots = snapshots;
        }

        public ISnapshot Restore()
        {
            Stack<ISnapshot> undoSnapshots = new Stack<ISnapshot>();

            while ( this.snapshots.Count > 0 )
            {
                undoSnapshots.Push(this.snapshots.Pop().Restore());
            }

            return new BatchSnapshot( undoSnapshots );
        }

        public bool IsNamedRestorePoint { get; private set; }

        public string Name { get; private set; }

        public ITrackable SnapshotTarget { get; private set; }

        public void ConvertToNamedRestorePoint( string name )
        {
            throw new System.NotImplementedException();
        }
    }
}