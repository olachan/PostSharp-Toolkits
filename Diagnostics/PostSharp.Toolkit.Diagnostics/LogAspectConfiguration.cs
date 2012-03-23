using System;
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
