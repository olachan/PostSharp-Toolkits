#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public abstract class Snapshot : ISnapshot
    {
        protected WeakReference<ITrackable> Target;

        protected Snapshot(ITrackable target)
        {
            this.Target = new WeakReference<ITrackable>(target);
        }

        protected Snapshot(ITrackable target, string restorePointName)
            : this(target)
        {
            this.IsNamedRestorePoint = true;
            this.Name = restorePointName;
        }

        public ITrackable SnapshotTarget { get { return this.Target.Target; } }

        public abstract ISnapshot Restore();

        public bool IsNamedRestorePoint { get; private set; }

        public string Name { get; private set; }

        public void ConvertToNamedRestorePoint( string name )
        {
            IsNamedRestorePoint = true;
            Name = name;
        }
    }
}