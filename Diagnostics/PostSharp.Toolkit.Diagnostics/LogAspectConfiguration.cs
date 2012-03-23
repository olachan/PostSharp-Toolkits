using System;
using PostSharp.Aspects.Configuration;

namespace PostSharp.Toolkit.Diagnostics
{
    public class LogAspectConfiguration : AspectConfiguration
    {
        public LogParametersOptions? OnEntryParametersOptions { get; set; }
        public LogParametersOptions? OnSuccessParametersOptions { get; set; }
        public LogParametersOptions? OnExceptionParametersOptions { get; set; }
        public LogLevel? OnEntryLogLevel { get; set; }
        public LogLevel? OnSuccessLogLevel { get; set; }
        public LogLevel? OnExceptionLogLevel { get; set; }
    }
}
