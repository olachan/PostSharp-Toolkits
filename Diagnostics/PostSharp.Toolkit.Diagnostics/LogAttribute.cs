using System;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Diagnostics
{
    [Serializable]
    [AttributeUsage(
      AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Module | AttributeTargets.Struct,
      AllowMultiple = true,
      Inherited = false)]
    [MulticastAttributeUsage(
    MulticastTargets.InstanceConstructor | MulticastTargets.StaticConstructor | MulticastTargets.Method,
      AllowMultiple = true)]
    [Metric("UsedFeatures", "Toolkit.Diagnostics.Logging")]
    //[AspectConfigurationAttributeType(typeof(LogAspectConfigurationAttribute))]
    //[LogAspectConfiguration(OnEntryLogParametersOptions = LogParametersOptions.IncludeParameterName | LogParametersOptions.IncludeParameterType | LogParametersOptions.IncludeParameterValue, OnExitLogOption = LogParametersOptions.None)]
    public class LogAttribute : MethodLevelAspect, ILogAspect, ILogAspectBuildSemantics
    {
#if !SMALL
        private LogParametersOptions? onEntryParametersOptions;
        public LogParametersOptions OnEntryLogParametersOptions
        {
            get { return this.onEntryParametersOptions.GetValueOrDefault(LogParametersOptions.None); }
            set { this.onEntryParametersOptions = value; }
        }

        private LogParametersOptions? onSuccessParametersOptions;
        public LogParametersOptions OnSuccessLogParametersOptions
        {
            get { return this.onSuccessParametersOptions.GetValueOrDefault(LogParametersOptions.None); }
            set { this.onSuccessParametersOptions = value; }
        }

        private LogParametersOptions? onExceptionParametersOptions;
        public LogParametersOptions OnExceptionLogParametersOptions
        {
            get { return this.onExceptionParametersOptions.GetValueOrDefault(LogParametersOptions.None); }
            set { this.onExceptionParametersOptions = value; }
        }

        private LogLevel? onEntryLogLevel;
        public LogLevel OnEntryLogLevel 
        {
            get { return this.onEntryLogLevel.GetValueOrDefault(LogLevel.None); }
            set { this.onEntryLogLevel = value; }
        }

        private LogLevel? onSuccessLogLevel;
        public LogLevel OnSuccessLogLevel
        {
            get { return this.onSuccessLogLevel.GetValueOrDefault(LogLevel.None); }
            set { this.onSuccessLogLevel = value; }
        }

        private LogLevel? onExceptionLogLevel;
        public LogLevel OnExceptionLogLevel
        {
            get { return this.onExceptionLogLevel.GetValueOrDefault(LogLevel.None); }
            set { this.onExceptionLogLevel = value; }
        }

        protected override AspectConfiguration CreateAspectConfiguration()
        {
            return new LogAspectConfiguration();
        }

        protected override void SetAspectConfiguration(AspectConfiguration aspectConfiguration, System.Reflection.MethodBase targetMethod)
        {
            LogAspectConfiguration configuration = (LogAspectConfiguration)aspectConfiguration;
            configuration.OnEntryParametersOptions = this.onEntryParametersOptions;
            configuration.OnSuccessParametersOptions = this.onSuccessParametersOptions;
            configuration.OnExceptionParametersOptions = this.onExceptionParametersOptions;
            configuration.OnEntryLogLevel = this.onEntryLogLevel;
            configuration.OnSuccessLogLevel = this.onSuccessLogLevel;
            configuration.OnExceptionLogLevel = this.onExceptionLogLevel;
        }
#endif
    }

  
}