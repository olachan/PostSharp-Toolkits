using PostSharp.Aspects.Configuration;

namespace PostSharp.Toolkit.Diagnostics
{
    public class LogAspectConfigurationAttribute : AspectConfigurationAttribute
    {
        private LogOptions? onEntryOptions;
        public LogOptions OnEntryOptions
        {
            get { return this.onEntryOptions.GetValueOrDefault(LogOptions.None); }
            set { this.onEntryOptions = value; }
        }

        private LogOptions? onSuccessOptions;
        public LogOptions OnSuccessOptions
        {
            get { return this.onSuccessOptions.GetValueOrDefault(LogOptions.None); }
            set { this.onSuccessOptions = value; }
        }

        private LogOptions? onExceptionOptions;
        public LogOptions OnExceptionOptions
        {
            get { return this.onExceptionOptions.GetValueOrDefault(LogOptions.None); }
            set { this.onExceptionOptions = value; }
        }

        private LogLevel? onEntryLevel;
        public LogLevel OnEntryLevel
        {
            get { return this.onEntryLevel.GetValueOrDefault(LogLevel.None); }
            set { this.onEntryLevel = value; }
        }

        private LogLevel? onSuccessLevel;
        public LogLevel OnSuccessLevel
        {
            get { return this.onSuccessLevel.GetValueOrDefault(LogLevel.None); }
            set { this.onSuccessLevel = value; }
        }

        private LogLevel? onExceptionLevel;
        public LogLevel OnExceptionLevel
        {
            get { return this.onExceptionLevel.GetValueOrDefault(LogLevel.None); }
            set { this.onExceptionLevel = value; }
        }

        protected override void SetAspectConfiguration(AspectConfiguration aspectConfiguration)
        {
            base.SetAspectConfiguration(aspectConfiguration);

            LogAspectConfiguration configuration = (LogAspectConfiguration)aspectConfiguration;
            configuration.OnEntryOptions = this.onEntryOptions;
            configuration.OnSuccessOptions = this.onSuccessOptions;
            configuration.OnExceptionOptions = this.onExceptionOptions;
            configuration.OnEntryLevel = this.onEntryLevel;
            configuration.OnSuccessLevel = this.onSuccessLevel;
            configuration.OnExceptionLevel = this.onExceptionLevel;
        }

        protected override AspectConfiguration CreateAspectConfiguration()
        {
            return new LogAspectConfiguration();
        }
    }
}