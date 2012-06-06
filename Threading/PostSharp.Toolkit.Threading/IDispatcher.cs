#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Threading;

namespace PostSharp.Toolkit.Threading
{
    public interface IDispatcher
    {
        SynchronizationContext SynchronizationContext { get; }
        bool CheckAccess();
        void Invoke( IAction action );
        void BeginInvoke( IAction action );
    }
}