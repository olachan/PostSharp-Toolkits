#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public abstract class Operation : IOperation
    {
        protected ITrackable Target;

        protected Operation(ITrackable target)
        {
            this.Target = target;
        }

        protected Operation(ITrackable target, string restorePointName)
            : this(target)
        {
            this.IsNamedRestorePoint = true;
            this.Name = restorePointName;
        }

        public ITrackable OperationTarget { get { return this.Target; } }

        public abstract void Undo();

        public abstract void Redo();

        public bool IsNamedRestorePoint { get; private set; }

        public string Name { get; private set; }

        public void ConvertToNamedRestorePoint( string name )
        {
            IsNamedRestorePoint = true;
            Name = name;
        }
    }
}