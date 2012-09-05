using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: Restore point should not be an operation
    //Maybe operation should contain a collection of restore points?
    internal sealed class RestorePoint : IOperation
    {
        public string Name { get; private set; }

        public RestorePointToken Token { get; private set; }

        public void Undo()
        {
            //Do nothing
        }

        public void Redo()
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