#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    //TODO replace with aspect and add compile time check of property type (it has to be ITrackedObject)
    [AttributeUsage(AttributeTargets.Field)]
    public class TrackedPropertyAttribute : Attribute
    {
         
    }
}