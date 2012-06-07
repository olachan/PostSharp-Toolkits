#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

namespace PostSharp.Toolkit.Threading.DeadlockDetection
{
    internal class ThreadAbortToken
    {
        private readonly string message;

        public ThreadAbortToken( string message )
        {
            this.message = message;
        }

        public string Message
        {
            get { return this.message; }
        }
    }
}