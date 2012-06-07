#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Threading;

namespace PostSharp.Toolkit.Threading
{
    public sealed class DispatcherSynchronizationContext : SynchronizationContext
    {
        private readonly IDispatcher dispatcher;

        public DispatcherSynchronizationContext( IDispatcher dispatcher )
        {
            this.dispatcher = dispatcher;
        }

        public override void Post( SendOrPostCallback d, object state )
        {
            this.dispatcher.BeginInvoke( new SendOrPostCallbackAction( d, state ) );
        }

        public override void Send( SendOrPostCallback d, object state )
        {
            if ( this.dispatcher.CheckAccess() )
            {
                d( state );
            }
            else
            {
                this.dispatcher.Invoke( new SendOrPostCallbackAction( d, state ) );
            }
        }

        private sealed class SendOrPostCallbackAction : IAction
        {
            private readonly SendOrPostCallback callback;
            private readonly object state;


            public SendOrPostCallbackAction( SendOrPostCallback callback, object state )
            {
                this.callback = callback;
                this.state = state;
            }

            public void Invoke()
            {
                this.callback( this.state );
            }
        }
    }
}