#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading
{
    internal class WorkItem : IAction
    {
        private readonly IMethodBinding binding;
        private readonly Arguments arguments;
        private object instance;

        protected IMethodBinding Binding
        {
            get { return this.binding; }
        }

        protected Arguments Arguments
        {
            get { return this.arguments; }
        }

        protected object Instance
        {
            get { return this.instance; }
        }

        public WorkItem( MethodInterceptionArgs args, bool clone = false )
        {
            this.instance = args.Instance;
            this.binding = args.Binding;
            this.arguments = clone ? args.Arguments.Clone() : args.Arguments;
        }

        public void Invoke()
        {
            try
            {
                this.binding.Invoke( ref this.instance, this.arguments );
            }
            catch ( Exception e )
            {
                bool handled = false;
                this.OnException( e, ref handled );
                if (!handled) throw;
            }
        }

        protected virtual void OnException( Exception e, ref bool handled)
        {
            
        }

        public static readonly SendOrPostCallback SendOrPostCallbackDelegate = SendOrPostCallback;

        private static void SendOrPostCallback( object state )
        {
            ((WorkItem) state).Invoke();
        }
    }
}