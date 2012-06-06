#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Threading;

namespace PostSharp.Toolkit.Threading
{
    internal class SynchronizationContextWrapper : IDispatcher
    {
        private readonly SynchronizationContext synchronizationContext;

        public SynchronizationContextWrapper( SynchronizationContext synchronizationContext )
        {
            this.synchronizationContext = synchronizationContext;
        }

        public bool CheckAccess()
        {
            return SynchronizationContext.Current == this.synchronizationContext;
        }

        public void Invoke( IAction action )
        {
            this.synchronizationContext.Send( WorkItem.SendOrPostCallbackDelegate, action );
        }

        public void BeginInvoke( IAction action )
        {
            this.synchronizationContext.Post( WorkItem.SendOrPostCallbackDelegate, action );
        }
    }

    
}