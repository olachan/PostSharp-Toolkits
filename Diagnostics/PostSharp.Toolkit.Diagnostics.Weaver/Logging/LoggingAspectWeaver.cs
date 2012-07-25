#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Extensibility;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.AspectWeavers;
using PostSharp.Sdk.CodeModel;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    internal sealed class LoggingAspectWeaver : MethodLevelAspectWeaver
    {
        private static readonly LogAspectConfigurationAttribute defaultConfiguration = new LogAspectConfigurationAttribute
                                                                                           {
                                                                                               OnEntryOptions =
                                                                                                   LogOptions.IncludeParameterType |
                                                                                                   LogOptions.IncludeParameterName |
                                                                                                   LogOptions.IncludeParameterValue,
                                                                                               OnExceptionOptions = LogOptions.None,
                                                                                               OnSuccessOptions =
                                                                                                   LogOptions.IncludeParameterType |
                                                                                                   LogOptions.IncludeReturnValue,
                                                                                               OnEntryLevel = LogLevel.Debug,
                                                                                               OnSuccessLevel = LogLevel.Debug,
                                                                                               OnExceptionLevel = LogLevel.Warning
                                                                                           };

        private LoggingAspectTransformation transformation;

        private InstrumentationPlugIn instrumentationPlugIn;

        public LoggingAspectWeaver()
            : base( defaultConfiguration, MulticastTargets.Property | MulticastTargets.Method | MulticastTargets.Class )
        {
            this.RequiresRuntimeInstance = false;
            this.RequiresRuntimeReflectionObject = false;
        }

        protected override void Initialize()
        {
            base.Initialize();

            this.instrumentationPlugIn = (InstrumentationPlugIn) this.AspectWeaverTask.Project.Tasks[InstrumentationPlugIn.Name];
            this.transformation = new LoggingAspectTransformation( this, this.instrumentationPlugIn.Backend );

            ApplyWaivedEffects( this.transformation );
        }

        public override bool ValidateAspectInstance(AspectInstanceInfo aspectInstanceInfo)
        {
            IMethod targetMethod = (IMethod) aspectInstanceInfo.TargetElement;
            
            if ( targetMethod.IsAbstract )
            {
                InstrumentationMessageSource.Instance.Write( targetMethod, SeverityType.Error, "DIA002", new object[]{ targetMethod } );
                return false;
            }

            return true;

        }

        protected override AspectWeaverInstance CreateAspectWeaverInstance( AspectInstanceInfo aspectInstanceInfo )
        {
            return new LoggingAspectWeaverInstance( this, aspectInstanceInfo );
        }

        private class LoggingAspectWeaverInstance : MethodLevelAspectWeaverInstance
        {
            public LoggingAspectWeaverInstance( MethodLevelAspectWeaver aspectWeaver, AspectInstanceInfo aspectInstanceInfo )
                : base( aspectWeaver, aspectInstanceInfo )
            {
            }

            public override void ProvideAspectTransformations( AspectWeaverTransformationAdder adder )
            {
                LoggingAspectTransformation transformation = ((LoggingAspectWeaver) AspectWeaver).transformation;
                AspectWeaverTransformationInstance transformationInstance = transformation.CreateInstance( this );

                adder.Add( TargetElement, transformationInstance );
            }
        }
    }
}