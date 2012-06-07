#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using PostSharp.Extensibility;

#pragma warning disable 420

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// <para>
    /// Abstract base class for entities implementaing actors-based communication pattern.
    /// </para>
    /// <para>
    /// Actors are business entities that respond to synchronous messages (implemented as void methods calls),
    /// which are queued and processed one at a time.
    /// This ensures that while any thread can interact with an actor by sending it a request, its code is never
    /// executed in two threads simultanously.
    /// </para>
    /// <para>
    /// Since all Actor public methods are by default treated as part of actor's messaging pattern and executed asynchronously,
    /// they must be void and have no ref or out parameters.
    /// Methods that should not be part of the contract (i.e. should act like regular methods and be executed synchronously)
    /// should be marked with <see cref="ThreadSafeAttribute"/>.
    /// </para>
    /// <para>
    /// Several actors can share the same message queue by sharing the same instance of <see cref="IDispatcher"/> object
    /// (see <see cref="Actor(IDispacther)"/>).
    /// </para>
    /// </summary>
    [Actor( AttributeInheritance = MulticastInheritance.Strict )]
    public abstract class Actor : IDispatcherObject
    {
        // TODO: Compatibility with async/await: methods returning a Task should be handled properly.

        private readonly IDispatcher dispatcher;

        protected Actor()
            : this( null )
        {
        }

        protected Actor( IDispatcher dispatcher )
        {
            this.dispatcher = dispatcher ?? new ActorDispatcher();
        }


        IDispatcher IDispatcherObject.Dispatcher
        {
            get { return this.dispatcher; }
        }

        public IDispatcher Dispatcher
        {
            [ThreadSafe]
            get { return this.dispatcher; }
        }


        [ThreadSafe]
        internal virtual void CallOnException( Exception exception, ref bool handled )
        {
            this.OnException( exception, ref handled );
        }

        protected virtual void OnException( Exception exception, ref bool handled )
        {
        }


        public bool IsDisposed { [ThreadSafe]
        get; private set; }

        [ThreadSafe]
        public virtual void Dispose()
        {
            this.IsDisposed = true;
        }

        [ThreadSafe]
        public override bool Equals( object obj )
        {
            return base.Equals( obj );
        }

        [ThreadSafe]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [ThreadSafe]
        public override string ToString()
        {
            return base.ToString();
        }
    }
}