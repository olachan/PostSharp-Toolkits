#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion
namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface IOperationTrackable
    {
        Snapshot TakeSnapshot();
    }
}