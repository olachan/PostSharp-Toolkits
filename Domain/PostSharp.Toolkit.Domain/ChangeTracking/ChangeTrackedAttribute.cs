#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: Public class documentation!
    //TODO: Review error messages and other string for classes names after refactoring

    //TODO replace with aspect and add compile time check of property type (it has to be ITrackedObject) applicable on properties(auto)
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ChangeTrackedAttribute : Attribute
    {
         
    }
}