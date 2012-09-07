using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: Restore point should not be an operation
    //Maybe operation should contain a collection of restore points?
    internal sealed class RestorePoint : Operation
    {
        public RestorePointToken Token { get; private set; }

        protected internal override void Undo()
        {
            //Do nothing
        }

        protected internal override void Redo()
        {
            //Do nothing
        }

        public RestorePoint(string name)
        {
            this.Name = name;
            this.Token = new RestorePointToken(name);
        }
    }
}