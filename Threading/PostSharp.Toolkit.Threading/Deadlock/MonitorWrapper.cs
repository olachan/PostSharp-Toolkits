#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Threading;

namespace PostSharp.Toolkit.Threading.Deadlock
{
    [DeadlockDetectionPolicy.MonitorEnhancements]
    internal static class MonitorWrapper
    {
        public static void Enter( object lockObject )
        {
            Monitor.Enter( lockObject );
        }

        public static void Exit( object lockObject )
        {
            Monitor.Exit( lockObject );
        }
    }
}