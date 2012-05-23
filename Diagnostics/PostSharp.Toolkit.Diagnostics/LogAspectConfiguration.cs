#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Aspects.Configuration;

namespace PostSharp.Toolkit.Diagnostics
{
    public class LogAspectConfiguration : AspectConfiguration
    {
        public LogOptions? OnEntryOptions { get; set; }
        public LogOptions? OnSuccessOptions { get; set; }
        public LogOptions? OnExceptionOptions { get; set; }
        public LogLevel? OnEntryLevel { get; set; }
        public LogLevel? OnSuccessLevel { get; set; }
        public LogLevel? OnExceptionLevel { get; set; }
    }
}