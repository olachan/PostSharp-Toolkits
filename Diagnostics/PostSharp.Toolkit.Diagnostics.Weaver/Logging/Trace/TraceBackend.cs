using System;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging.Trace
{
    internal sealed class TraceBackend : ILoggingBackend
    {
        private ModuleDeclaration module;
        private LoggingImplementationTypeBuilder loggingImplementation;

        private IMethod writeLineString;
        private IMethod traceInfoString;
        private IMethod traceInfoFormat;
        private IMethod traceWarningString;
        private IMethod traceWarningFormat;
        private IMethod traceErrorString;
        private IMethod traceErrorFormat;

        public void Initialize(ModuleDeclaration module)
        {
            this.module = module;
            this.loggingImplementation = new LoggingImplementationTypeBuilder(module);

            ITypeSignature traceTypeSignature = module.Cache.GetType(typeof(System.Diagnostics.Trace));
            
            this.writeLineString = module.FindMethod(traceTypeSignature, "WriteLine",
                method => method.Parameters.Count == 1 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String));

            this.traceInfoString = module.FindMethod(traceTypeSignature, "TraceInformation",
                method => method.Parameters.Count == 1 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String));

            this.traceInfoFormat = module.FindMethod(traceTypeSignature, "TraceInformation",
                method => method.Parameters.Count == 2 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                          method.Parameters[1].ParameterType.BelongsToClassification(TypeClassifications.Array));

            this.traceWarningString = module.FindMethod(traceTypeSignature, "TraceWarning",
                method => method.Parameters.Count == 1 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String));

            this.traceWarningFormat = module.FindMethod(traceTypeSignature, "TraceWarning",
                method => method.Parameters.Count == 2 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                          method.Parameters[1].ParameterType.BelongsToClassification(TypeClassifications.Array));

            this.traceErrorString = module.FindMethod(traceTypeSignature, "TraceError",
                method => method.Parameters.Count == 1 &&
                IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String));

            this.traceErrorFormat = module.FindMethod(traceTypeSignature, "TraceError",
                method => method.Parameters.Count == 2 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                          method.Parameters[1].ParameterType.BelongsToClassification(TypeClassifications.Array));
        }

        public ILoggingBackendInstance CreateInstance(AspectWeaverInstance aspectWeaverInstance)
        {
            return new TraceBackendInstance(this);
        }

        private class TraceBackendInstance : ILoggingBackendInstance
        {
            private readonly TraceBackend parent;

            public TraceBackendInstance(TraceBackend parent)
            {
                this.parent = parent;
            }

            public ILoggingCategoryBuilder GetCategoryBuilder(string categoryName)
            {
                return new TraceCategoryBuilder(this.parent);
            }
        }

        private class TraceCategoryBuilder : ILoggingCategoryBuilder
        {
            private readonly TraceBackend parent;

            public TraceCategoryBuilder(TraceBackend parent)
            {
                this.parent = parent;
            }

            public bool SupportsIsEnabled
            {
                get { return false; }
            }

            public void EmitGetIsEnabled(InstructionWriter writer, LogLevel logLevel)
            {
            }

            public void EmitWrite(InstructionWriter writer, string messageFormattingString, int argumentsCount, LogLevel logLevel, 
                                  Action<InstructionWriter> getExceptionAction, Action<int, InstructionWriter> loadArgumentAction, bool useWrapper)
            {
                bool createArgsArray = argumentsCount > 0;
                bool useStringFormat = false;

                IMethod method;

                switch (logLevel)
                {
                    case LogLevel.Debug:
                        useStringFormat = createArgsArray;

                        method = useStringFormat
                                     ? this.parent.loggingImplementation.GetTraceStringFormatMethod("Trace")
                                     : this.parent.writeLineString;
                        break;
                    case LogLevel.Info:
                        method = createArgsArray ? this.parent.traceInfoFormat : this.parent.traceInfoString;
                        break;
                    case LogLevel.Warning:
                        method = createArgsArray ? this.parent.traceWarningFormat : this.parent.traceWarningString;
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        method = createArgsArray ? this.parent.traceErrorFormat : this.parent.traceErrorString;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("logLevel");
                }

                if (getExceptionAction != null)
                {
                    getExceptionAction(writer);
                }
                writer.EmitInstructionString(OpCodeNumber.Ldstr, messageFormattingString);

                if (createArgsArray)
                {
                    writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, argumentsCount);
                    writer.EmitInstructionType(OpCodeNumber.Newarr,
                                               this.parent.module.Cache.GetIntrinsicBoxedType(IntrinsicType.Object));
                }

                for (int i = 0; i < argumentsCount; i++)
                {
                    if (createArgsArray)
                    {
                        writer.EmitInstruction(OpCodeNumber.Dup);
                        writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, i);
                    }

                    if (loadArgumentAction != null)
                    {
                        loadArgumentAction(i, writer);
                    }

                    if (createArgsArray)
                    {
                        writer.EmitInstruction(OpCodeNumber.Stelem_Ref);
                    }
                }

                if (useWrapper)
                {
                    method = this.parent.loggingImplementation.GetWriteWrapperMethod(method.Name, method);
                }

                writer.EmitInstructionMethod(OpCodeNumber.Call, method);
            }
        }
    }
}