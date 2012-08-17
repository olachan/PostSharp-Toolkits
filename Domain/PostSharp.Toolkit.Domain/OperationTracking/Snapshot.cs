#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public abstract class Snapshot
    {
        protected WeakReference Target;

        protected Snapshot(IOperationTrackable target)
        {
            this.Target = new WeakReference( target );
        }

        public IOperationTrackable SnapshotTarget
        {
            get
            {
                return this.Target.Target as IOperationTrackable;
            }
        }

        public abstract void Restore();

        //{
        //    IOperationTrackable target = Target.Target as IOperationTrackable;
        //    if (target != null)
        //    {
        //        target.RestoreSnapshot( this );
        //    }
        //}
    }
}