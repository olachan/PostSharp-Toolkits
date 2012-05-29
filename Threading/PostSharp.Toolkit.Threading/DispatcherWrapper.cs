#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Windows.Threading;

namespace PostSharp.Toolkit.Threading
{
    internal class DispatcherWrapper : IDispatcher
    {
        private readonly Dispatcher dispatcher;

        public DispatcherWrapper( Dispatcher dispatcher )
        {
            this.dispatcher = dispatcher;
        }

        public bool CheckAccess()
        {
            return this.dispatcher.CheckAccess();
        }

        public void Invoke( IAction action )
        {
            this.dispatcher.Invoke( new Action( action.Invoke ), DispatcherPriority.Normal );
        }

        public void BeginInvoke( IAction action )
        {
            this.dispatcher.BeginInvoke( new Action( action.Invoke ), DispatcherPriority.Normal );
        }
    }
}