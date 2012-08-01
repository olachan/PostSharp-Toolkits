using System;

using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading
{
    internal class WorkItemWithExceptionInterceptor : WorkItem
    {
        public Exception Exception { get; private set; }

        public bool HasError { get; private set; }

        public WorkItemWithExceptionInterceptor( MethodInterceptionArgs args, bool clone = false )
            : base( args, clone )
        {
        }

        protected override void OnException(Exception e, ref bool handled)
        {
            HasError = true;
            Exception = e;
            handled = true;
        }
    }
}
