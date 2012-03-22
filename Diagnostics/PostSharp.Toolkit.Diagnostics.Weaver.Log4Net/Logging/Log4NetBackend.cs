using System;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Toolkit.Diagnostics.Weaver.Logging;
using log4net;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Log4Net.Logging
{
    internal sealed class Log4NetBackend : ILoggingBackend
    {
        private LoggingImplementationTypeBuilder loggingImplementation;
        private StringFormatWriter formatWriter;
        private ModuleDeclaration module;

        private IMethod writeDebugMethod;
        private IMethod writeDebugFormat1Method;
        private IMethod writeDebugFormat2Method;
        private IMethod writeDebugFormat3Method;
        private IMethod writeDebugFormatArrayMethod;
        private IMethod writeInfoMethod;
        private IMethod writeInfoFormat1Method;
        private IMethod writeInfoFormat2Method;
        private IMethod writeInfoFormat3Method;
        private IMethod writeInfoFormatArrayMethod;
        private IMethod writeWarningMethod;
        private IMethod writeWarningFormat1Method;
        private IMethod writeWarningFormat2Method;
        private IMethod writeWarningFormat3Method;
        private IMethod writeWarningFormatArrayMethod;
        private IMethod writeErrorMethod;
        private IMethod writeErrorFormat1Method;
        private IMethod writeErrorFormat2Method;
        private IMethod writeErrorFormat3Method;
        private IMethod writeErrorFormatArrayMethod;
        private IMethod writeFatalMethod;
        private IMethod writeFatalFormat1Method;
        private IMethod writeFatalFormat2Method;
        private IMethod writeFatalFormat3Method;
        private IMethod writeFatalFormatArrayMethod;
        
        private IMethod getIsDebugEnabledMethod;
        private IMethod getIsInfoEnabledMethod;
        private IMethod getIsWarnEnabledMethod;
        private IMethod getIsErrorEnabledMethod;
        private IMethod getIsFatalEnabledMethod;
        private IMethod categoryInitializerMethod;
        
        private ITypeSignature loggerType;
        
        private readonly Predicate<MethodDefDeclaration> format1Predicate;
        private readonly Predicate<MethodDefDeclaration> format2Predicate;
        private readonly Predicate<MethodDefDeclaration> format3Predicate;
        private readonly Predicate<MethodDefDeclaration> formatArrayPredicate;

        public Log4NetBackend()
        {
            this.format1Predicate = method => method.Parameters.Count == 2 &&
                IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                IntrinsicTypeSignature.Is(method.Parameters[1].ParameterType, IntrinsicType.Object);

            this.format2Predicate = method => method.Parameters.Count == 3 &&
                IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                IntrinsicTypeSignature.Is(method.Parameters[1].ParameterType, IntrinsicType.Object) &&
                IntrinsicTypeSignature.Is(method.Parameters[2].ParameterType, IntrinsicType.Object);

            this.format3Predicate = method => method.Parameters.Count == 4 &&
                IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                IntrinsicTypeSignature.Is(method.Parameters[1].ParameterType, IntrinsicType.Object) &&
                IntrinsicTypeSignature.Is(method.Parameters[2].ParameterType, IntrinsicType.Object) &&
                IntrinsicTypeSignature.Is(method.Parameters[3].ParameterType, IntrinsicType.Object);

            this.formatArrayPredicate = method => method.Parameters.Count == 2 &&
                IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                method.Parameters[1].ParameterType. BelongsToClassification(TypeClassifications.Array);
        }

        public void Initialize(ModuleDeclaration module)
        {
            this.module = module;
            this.loggingImplementation = new LoggingImplementationTypeBuilder(module);
            this.formatWriter = new StringFormatWriter(module);
            this.loggerType = module.FindType(typeof(ILog));

            this.categoryInitializerMethod = module.FindMethod(module.FindType(typeof(LogManager)), "GetLogger",
                method => method.Parameters.Count == 1 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String));

            this.writeDebugMethod = module.FindMethod(this.loggerType, "Debug", 1);
            this.writeDebugFormat1Method = module.FindMethod(this.loggerType, "DebugFormat", this.format1Predicate);
            this.writeDebugFormat2Method = module.FindMethod(this.loggerType, "DebugFormat", this.format2Predicate);
            this.writeDebugFormat3Method = module.FindMethod(this.loggerType, "DebugFormat", this.format3Predicate);
            this.writeDebugFormatArrayMethod = module.FindMethod(this.loggerType, "DebugFormat", this.formatArrayPredicate);

            this.writeInfoMethod = module.FindMethod(this.loggerType, "Info", 1);
            this.writeInfoFormat1Method = module.FindMethod(this.loggerType, "InfoFormat", this.format1Predicate);
            this.writeInfoFormat2Method = module.FindMethod(this.loggerType, "InfoFormat", this.format2Predicate);
            this.writeInfoFormat3Method = module.FindMethod(this.loggerType, "InfoFormat", this.format3Predicate);
            this.writeInfoFormatArrayMethod = module.FindMethod(this.loggerType, "InfoFormat", this.formatArrayPredicate);

            this.writeWarningMethod = module.FindMethod(this.loggerType, "Warn", 1);
            this.writeWarningFormat1Method = module.FindMethod(this.loggerType, "WarnFormat", this.format1Predicate);
            this.writeWarningFormat2Method = module.FindMethod(this.loggerType, "WarnFormat", this.format2Predicate);
            this.writeWarningFormat3Method = module.FindMethod(this.loggerType, "WarnFormat", this.format3Predicate);
            this.writeWarningFormatArrayMethod = module.FindMethod(this.loggerType, "WarnFormat", this.formatArrayPredicate);

            this.writeErrorMethod = module.FindMethod(this.loggerType, "Error", 1);
            this.writeErrorFormat1Method = module.FindMethod(this.loggerType, "ErrorFormat", this.format1Predicate);
            this.writeErrorFormat2Method = module.FindMethod(this.loggerType, "ErrorFormat", this.format2Predicate);
            this.writeErrorFormat3Method = module.FindMethod(this.loggerType, "ErrorFormat", this.format3Predicate);
            this.writeErrorFormatArrayMethod = module.FindMethod(this.loggerType, "ErrorFormat", this.formatArrayPredicate);

            this.writeFatalMethod = module.FindMethod(this.loggerType, "Fatal", 1);
            this.writeFatalFormat1Method = module.FindMethod(this.loggerType, "FatalFormat", this.format1Predicate);
            this.writeFatalFormat2Method = module.FindMethod(this.loggerType, "FatalFormat", this.format2Predicate);
            this.writeFatalFormat3Method = module.FindMethod(this.loggerType, "FatalFormat", this.format3Predicate);
            this.writeFatalFormatArrayMethod = module.FindMethod(this.loggerType, "FatalFormat", this.formatArrayPredicate);

            this.getIsDebugEnabledMethod = module.FindMethod(this.loggerType, "get_IsDebugEnabled");
            this.getIsInfoEnabledMethod = module.FindMethod(this.loggerType, "get_IsInfoEnabled");
            this.getIsWarnEnabledMethod = module.FindMethod(this.loggerType, "get_IsWarnEnabled");
            this.getIsErrorEnabledMethod = module.FindMethod(this.loggerType, "get_IsErrorEnabled");
            this.getIsFatalEnabledMethod = module.FindMethod(this.loggerType, "get_IsFatalEnabled");
        }

        public ILoggingBackendInstance CreateInstance(AspectWeaverInstance aspectWeaverInstance)
        {
            return new Log4NetBackendInstance(this, aspectWeaverInstance.AspectType.Module);
        }

        private class Log4NetBackendInstance : ILoggingBackendInstance
        {
            private readonly Log4NetBackend parent;

            public Log4NetBackendInstance(Log4NetBackend parent, ModuleDeclaration module)
            {
                this.parent = parent;
            }

            public ILoggingCategoryBuilder GetCategoryBuilder(string categoryName)
            {
                return new Log4NetCategoryBuilder(this.parent, categoryName);
            }
        }

        private class Log4NetCategoryBuilder : ILoggingCategoryBuilder
        {
            private readonly Log4NetBackend parent;
            private readonly FieldDefDeclaration loggerField;

            public Log4NetCategoryBuilder(Log4NetBackend parent, string categoryName)
            {
                this.parent = parent;

                this.loggerField = this.parent.loggingImplementation.GetCategoryField(categoryName, this.parent.loggerType, writer =>
                {
                    writer.EmitInstructionString(OpCodeNumber.Ldstr, categoryName);
                    writer.EmitInstructionMethod(OpCodeNumber.Call, this.parent.categoryInitializerMethod);
                });
            }

            public bool SupportsIsEnabled
            {
                get { return true; }
            }

            public void EmitGetIsEnabled(InstructionWriter writer, LogSeverity logSeverity)
            {
                writer.EmitInstructionField(OpCodeNumber.Ldsfld, this.loggerField);

                switch (logSeverity)
                {
                    case LogSeverity.Trace:
                        writer.EmitInstructionMethod(OpCodeNumber.Callvirt, this.parent.getIsDebugEnabledMethod);
                        break;
                    case LogSeverity.Info:
                        writer.EmitInstructionMethod(OpCodeNumber.Callvirt, this.parent.getIsInfoEnabledMethod);
                        break;
                    case LogSeverity.Warning:
                        writer.EmitInstructionMethod(OpCodeNumber.Callvirt, this.parent.getIsWarnEnabledMethod);
                        break;
                    case LogSeverity.Error:
                        writer.EmitInstructionMethod(OpCodeNumber.Callvirt, this.parent.getIsErrorEnabledMethod);
                        break;
                    case LogSeverity.Fatal:
                        writer.EmitInstructionMethod(OpCodeNumber.Callvirt, this.parent.getIsFatalEnabledMethod);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("logSeverity");
                }
            }

            public void EmitWrite(InstructionWriter writer, string messageFormattingString, int argumentsCount, 
                                  LogSeverity logSeverity, Action<InstructionWriter> getExceptionAction,
                                  Action<int, InstructionWriter> loadArgumentAction, bool useWrapper)
            {
                bool createArgsArray;
                IMethod method = this.GetLoggerMethod(argumentsCount, logSeverity, out createArgsArray);

                if (getExceptionAction != null)
                {
                    getExceptionAction(writer);
                }

                writer.EmitInstructionField(OpCodeNumber.Ldsfld, this.loggerField);
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

            // todo refactor this
            private IMethod GetLoggerMethod(int argumentsCount, LogSeverity logSeverity, out bool createArgsArray)
            {
                createArgsArray = false;
                IMethod method;
                switch (logSeverity)
                {
                    case LogSeverity.Trace:
                        switch (argumentsCount)
                        {
                            case 0:
                                method = this.parent.writeDebugMethod;
                                break;
                            case 1:
                                method = this.parent.writeDebugFormat1Method;
                                break;
                            case 2:
                                method = this.parent.writeDebugFormat2Method;
                                break;
                            case 3:
                                method = this.parent.writeDebugFormat3Method;
                                break;
                            default:
                                method = this.parent.writeDebugFormatArrayMethod;
                                createArgsArray = true;
                                break;
                        }
                        break;
                    case LogSeverity.Info:
                        switch (argumentsCount)
                        {
                            case 0:
                                method = this.parent.writeInfoMethod;
                                break;
                            case 1:
                                method = this.parent.writeInfoFormat1Method;
                                break;
                            case 2:
                                method = this.parent.writeInfoFormat2Method;
                                break;
                            case 3:
                                method = this.parent.writeInfoFormat3Method;
                                break;
                            default:
                                method = this.parent.writeInfoFormatArrayMethod;
                                createArgsArray = true;
                                break;
                        }
                        break;
                    case LogSeverity.Warning:
                        switch (argumentsCount)
                        {
                            case 0:
                                method = this.parent.writeWarningMethod;
                                break;
                            case 1:
                                method = this.parent.writeWarningFormat1Method;
                                break;
                            case 2:
                                method = this.parent.writeWarningFormat2Method;
                                break;
                            case 3:
                                method = this.parent.writeWarningFormat3Method;
                                break;
                            default:
                                method = this.parent.writeWarningFormatArrayMethod;
                                createArgsArray = true;
                                break;
                        }
                        break;
                    case LogSeverity.Error:
                        switch (argumentsCount)
                        {
                            case 0:
                                method = this.parent.writeErrorMethod;
                                break;
                            case 1:
                                method = this.parent.writeErrorFormat1Method;
                                break;
                            case 2:
                                method = this.parent.writeErrorFormat2Method;
                                break;
                            case 3:
                                method = this.parent.writeErrorFormat3Method;
                                break;
                            default:
                                method = this.parent.writeErrorFormatArrayMethod;
                                createArgsArray = true;
                                break;
                        }
                        break;
                    case LogSeverity.Fatal:
                        switch (argumentsCount)
                        {
                            case 0:
                                method = this.parent.writeFatalMethod;
                                break;
                            case 1:
                                method = this.parent.writeFatalFormat1Method;
                                break;
                            case 2:
                                method = this.parent.writeFatalFormat2Method;
                                break;
                            case 3:
                                method = this.parent.writeFatalFormat3Method;
                                break;
                            default:
                                method = this.parent.writeFatalFormatArrayMethod;
                                createArgsArray = true;
                                break;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("logSeverity");
                }
                return method;
            }
        }
    }
}