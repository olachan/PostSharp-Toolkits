using System;
using System.Collections;
using System.Collections.Generic;

using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal class SnapshotCollection : ISnapshotCollection
    {
        //private int currentItemIndex = 0;
        private readonly Stack<ISnapshot> snapshots;

        private readonly Dictionary<string, int> namedRestorePoints; 

        public SnapshotCollection()
        {
            this.namedRestorePoints = new Dictionary<string, int>();
            this.snapshots = new Stack<ISnapshot>();
        }

        public void Push(ISnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            //this.currentItemIndex++;
            this.snapshots.Push( snapshot );

            if (snapshot.IsNamedRestorePoint)
            {
                this.AddNamedRestorePoint( snapshot );
            }
        }

        public ISnapshot Pop()
        {
            if (this.snapshots.Count == 0)
            {
                return null;
            }

            ISnapshot snapshot = this.snapshots.Pop();

            if ( !snapshot.IsNamedRestorePoint )
            {
                return snapshot;
            }

            this.DecreaseRestorePointCount(snapshot.Name);

            return snapshot; //this.snapshots.Pop();
        }

        private void DecreaseRestorePointCount( string name )
        {
            this.namedRestorePoints[name] -= 1;

            if (this.namedRestorePoints[name] == 0)
            {
                this.namedRestorePoints.Remove(name);
            }
        }

        public void AddNamedRestorePoint(string name)
        {
            this.Push( new EmptyNamedRestorePoint( name ) );
        }

        private void AddNamedRestorePoint(ISnapshot restorePoint)
        {
            int namedRestorePointCount;

            if (!namedRestorePoints.TryGetValue(restorePoint.Name, out namedRestorePointCount))
            {
                namedRestorePointCount = 0;
            }

            namedRestorePoints.AddOrUpdate(restorePoint.Name, namedRestorePointCount + 1);
        }

        public Stack<ISnapshot> GetSnapshotsToRestorePoint(string name)
        {
            if (!namedRestorePoints.ContainsKey( name ))
            {
                throw new ArgumentException(string.Format("No restore point named {0}", name));
            }

            this.DecreaseRestorePointCount(name);

            List<ISnapshot> restoreSnapshots = new List<ISnapshot>();

            ISnapshot restorePoint = null;

            // TODO performance optimization
            while (restorePoint == null || restorePoint.Name != name)
            {
                restorePoint = this.snapshots.Pop();
                restoreSnapshots.Add(restorePoint);
            }

            restoreSnapshots.Reverse();

            return new Stack<ISnapshot>(restoreSnapshots);
        }

        public void Clear()
        {
            this.snapshots.Clear();
            //this.namedRestorePoints.Clear();
            //this.currentItemIndex = 0;
        }

        public sealed class EmptyNamedRestorePoint : ISnapshot
        {
            public bool IsNamedRestorePoint { get { return true; } }

            public string Name { get; private set; }

            public void ConvertToNamedRestorePoint( string name )
            {
                Name = name;
            }

            public EmptyNamedRestorePoint(string name)
            {
                Name = name;
            }

            public ISnapshot Restore()
            {
                return this;
            }
        }
    }
}