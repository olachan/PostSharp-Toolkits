#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;

using PostSharp.Toolkit.Diagnostics;

namespace PostSharp.Toolkit.Tests
{
    [Log(OnExceptionOptions = LogOptions.IncludeParameterName | LogOptions.IncludeParameterType| LogOptions.IncludeParameterValue, OnExceptionLevel = LogLevel.Warning)]
    public class ExceptionThrowingClass
    {
         public void ThrowException(int i)
         {
             throw new Exception("Test exception");
         }
    }
}