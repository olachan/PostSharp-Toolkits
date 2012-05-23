#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Sdk.AspectWeaver;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    internal sealed class ConfigurationOptions
    {
        private readonly LogOptions onEntryOptions;
        private readonly LogOptions onSuccessOptions;
        private readonly LogOptions onExceptionOptions;
        private readonly LogLevel onEntryLevel;
        private readonly LogLevel onSuccessLevel;
        private readonly LogLevel onExceptionLevel;

        public LogOptions OnEntryOptions
        {
            get { return this.onEntryOptions; }
        }

        public LogOptions OnSuccessOptions
        {
            get { return this.onSuccessOptions; }
        }

        public LogOptions OnExceptionOptions
        {
            get { return this.onExceptionOptions; }
        }

        public LogLevel OnEntryLevel
        {
            get { return this.onEntryLevel; }
        }

        public LogLevel OnSuccessLevel
        {
            get { return this.onSuccessLevel; }
        }

        public LogLevel OnExceptionLevel
        {
            get { return this.onExceptionLevel; }
        }

        public ConfigurationOptions( AspectWeaverInstance weaverInstance )
        {
            this.onEntryOptions = weaverInstance.GetConfigurationValue<LogAspectConfiguration, LogOptions>( c => c.OnEntryOptions );
            this.onSuccessOptions = weaverInstance.GetConfigurationValue<LogAspectConfiguration, LogOptions>( c => c.OnSuccessOptions );
            this.onExceptionOptions = weaverInstance.GetConfigurationValue<LogAspectConfiguration, LogOptions>( c => c.OnExceptionOptions );
            this.onEntryLevel = weaverInstance.GetConfigurationValue<LogAspectConfiguration, LogLevel>( c => c.OnEntryLevel );
            this.onSuccessLevel = weaverInstance.GetConfigurationValue<LogAspectConfiguration, LogLevel>( c => c.OnSuccessLevel );
            this.onExceptionLevel = weaverInstance.GetConfigurationValue<LogAspectConfiguration, LogLevel>( c => c.OnExceptionLevel );
        }
    }
}