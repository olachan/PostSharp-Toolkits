using System;
using System.Collections.Generic;

namespace PostSharp.Toolkit.INPC
{
    internal interface INestableContext
    {
        void Push( NestableContextInfo  context);
        NestableContextInfo Pop();

        NestableContextInfo Current { get; }
    }


    internal class NestableContext<TContextInfo> : INestableContext
        where TContextInfo : NestableContextInfo, new()
    {
        private readonly Stack<TContextInfo> contextsStack = new Stack<TContextInfo>();

        NestableContextInfo INestableContext.Pop()
        {
            return this.Pop();
        }

        NestableContextInfo INestableContext.Current
        {
            get { return Current; }
        }

        public TContextInfo Current
        {
            get
            {
                if (this.contextsStack.Count == 0) return null;
                return this.contextsStack.Peek();
            }
        }

        private void Push (TContextInfo context)
        {
            this.contextsStack.Push( context );
        }

        void INestableContext.Push( NestableContextInfo context )
        {
            this.Push( (TContextInfo) context );
        }

        private TContextInfo Pop()
        {
            return this.contextsStack.Pop();
        }

        public TContextInfo InContext(Func<TContextInfo> factory = null)
        {
            TContextInfo context = (factory == null) ? new TContextInfo() : factory();
            this.contextsStack.Push( context );
            context.RegisterOwner( this );
            return context;
        }
    }

    
    internal abstract class NestableContextInfo : IDisposable
    {
        private INestableContext owner;
        
        internal void RegisterOwner(INestableContext owner)
        {
            this.owner = owner;
        }

        public void Dispose()
        {
            if (this.owner != null)
            {
                this.owner.Pop();
            }
        }
    }
}