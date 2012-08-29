#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion
namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: What does it really mean? It's implemented by both trackers and tracked objects...
    public interface ITrackable
    {
        //[DoNotMakeAutomaticSnapshot]
        // IOperation TakeSnapshot();
    }
}