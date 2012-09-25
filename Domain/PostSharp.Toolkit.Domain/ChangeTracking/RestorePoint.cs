using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
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