using System;

namespace PostSharp.Toolkit.Domain
{
    internal abstract class NestableContextInfo : IDisposable
    {
        private INestableContext owner;

        internal void RegisterOwner( INestableContext owner )
        {
            this.owner = owner;
        }

        public void Dispose()
        {
            if ( this.owner != null )
            {
                this.owner.Pop();
            }
        }
    }
}