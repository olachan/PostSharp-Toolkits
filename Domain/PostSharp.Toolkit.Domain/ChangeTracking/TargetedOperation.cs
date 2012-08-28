#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: Don't like the name; Update: don't like the class at all, does nothing
    //TODO: Do we really need it?
    public abstract class TargetedOperation : IOperation
    {
        protected readonly ITrackable Target;
        
        protected TargetedOperation(ITrackable target, string name = null)
        {
            this.Target = target;
            this.Name = name;
        }

        public ITrackable OperationTarget { get { return this.Target; } }

        public abstract void Undo();

        public abstract void Redo();

        public string Name { get; private set; }
    }
}