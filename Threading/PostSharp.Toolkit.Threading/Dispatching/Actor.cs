#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PostSharp.Extensibility;

#pragma warning disable 420

namespace PostSharp.Toolkit.Threading.Dispatching
{
    [Actor(AttributeInheritance = MulticastInheritance.Strict)]
    public abstract class Actor : IDispatcherObject
    {
        // TODO: Compatibility with async/await: methods returning a Task should be handled properly.

        private readonly IDispatcher dispatcher;

        protected Actor()
            : this(null)
        {
        }

        protected Actor(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? new ActorDispatcher();
        }


        IDispatcher IDispatcherObject.Dispatcher
        {
            get { return this.dispatcher; }
        }

       
        [ThreadSafe]
        internal virtual void CallOnException(Exception exception, ref bool handled)
        {
            this.OnException( exception, ref handled );
        }

        protected virtual void OnException(Exception exception, ref bool handled)
        {
        }


        public bool IsDisposed
        {
            [ThreadSafe]
            get;
            private set;
        }

        [ThreadSafe]
        public virtual void Dispose()
        {
            this.IsDisposed = true;
        }
    }
}