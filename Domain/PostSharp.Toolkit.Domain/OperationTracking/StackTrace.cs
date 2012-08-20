#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Threading;

using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal static class StackTrace
    {
        private static readonly ThreadLocal<StackContext> stackTrace = new ThreadLocal<StackContext>( () => new StackContext() );

        private static StackContext StackContext
        {
            get
            {
                return stackTrace.Value;
            }
        }
        
        public static void PushOnStack(object o)
        {
            StackContext.PushOnStack(o);
        }

        public static object PopFromStack()
        {
            return StackContext.Pop();
        }

        public static object StackPeek()
        {
            return StackContext.Count == 0 ? null : StackContext.Peek();
        }
    }
}