using System;

namespace PostSharp.Toolkit.Domain
{
    internal abstract class NestableContextInfo : IDisposable
    {
        protected INestableContext Owner;

        internal void RegisterOwner( INestableContext owner )
        {
            this.Owner = owner;
        }

        public void Dispose()
        {
            if ( this.Owner != null )
            {
                this.Owner.Pop();
            }
        }
    }
}