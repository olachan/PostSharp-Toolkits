using System.Collections;
using System.Collections.Generic;

using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal class SnapshotCollection
    {
        private int currentItemIndex = 0;
        private readonly Stack<Snapshot> snapshots;

        private readonly Dictionary<string, Stack<int>> namedRestorePoints; 

        public SnapshotCollection()
        {
            this.namedRestorePoints = new Dictionary<string, Stack<int>>();
            this.snapshots = new Stack<Snapshot>();
        }

        public void Push(Snapshot snapshot)
        {
            this.currentItemIndex++;
            this.snapshots.Push( snapshot );
        }

        public Snapshot Pop()
        {
            this.currentItemIndex--;
            return this.snapshots.Pop();
        }

        public void AddNamedRestorePoint(string name)
        {
            Stack<int> indexStack = this.namedRestorePoints.GetOrCreate( name, () => new Stack<int>() );
            indexStack.Push( this.currentItemIndex );
        }

        public Stack<Snapshot> GetSnapshotsToRestorePoint(string name)
        {
            Stack<int> indexStack;
            if (!this.namedRestorePoints.TryGetValue( name, out indexStack ))
            {
                return null;
            }

            int restoreIndex = indexStack.Pop();
            List<Snapshot> restoreSnapshots = new List<Snapshot>();

            // TODO performance optimization
            while ( this.currentItemIndex > restoreIndex )
            {
                this.currentItemIndex--;
                restoreSnapshots.Add( this.snapshots.Pop() );
            }

            restoreSnapshots.Reverse();

            return new Stack<Snapshot>(restoreSnapshots);
        }

        public void Clear()
        {
            this.snapshots.Clear();
            this.namedRestorePoints.Clear();
            this.currentItemIndex = 0;
        }
    }
}