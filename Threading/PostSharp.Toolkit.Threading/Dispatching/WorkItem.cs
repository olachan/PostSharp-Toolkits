#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Threading;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    internal sealed class WorkItem : IAction
    {
        private readonly IMethodBinding binding;
        private readonly Arguments arguments;
        private object instance;

        public WorkItem( MethodInterceptionArgs args, bool clone = false )
        {
            this.instance = args.Instance;
            this.binding = args.Binding;
            this.arguments = clone ? args.Arguments.Clone() : args.Arguments;
        }

        public WorkItem( object instance, IMethodBinding binding, Arguments arguments )
        {
            this.instance = instance;
            this.binding = binding;
            this.arguments = arguments;
        }

        public void Invoke()
        {
            this.binding.Invoke( ref this.instance, this.arguments );
        }

        public static readonly SendOrPostCallback SendOrPostCallbackDelegate = SendOrPostCallback;

        private static void SendOrPostCallback( object state )
        {
            ((WorkItem) state).Invoke();
        }
    }
}