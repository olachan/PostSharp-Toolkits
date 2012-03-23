using PostSharp.Aspects.Configuration;

namespace PostSharp.Toolkit.Diagnostics
{
    public class LogAspectConfigurationAttribute : AspectConfigurationAttribute
    {
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

        protected override void SetAspectConfiguration(AspectConfiguration aspectConfiguration)
        {
            base.SetAspectConfiguration(aspectConfiguration);

            LogAspectConfiguration configuration = (LogAspectConfiguration)aspectConfiguration;
            configuration.OnEntryParametersOptions = this.onEntryParametersOptions;
            configuration.OnSuccessParametersOptions = this.onSuccessParametersOptions;
            configuration.OnExceptionParametersOptions = this.onExceptionParametersOptions;
            configuration.OnEntryLogLevel = this.onEntryLogLevel;
            configuration.OnSuccessLogLevel = this.onSuccessLogLevel;
            configuration.OnExceptionLogLevel = this.onExceptionLogLevel;
        }

        protected override AspectConfiguration CreateAspectConfiguration()
        {
            return new LogAspectConfiguration();
        }
    }
}