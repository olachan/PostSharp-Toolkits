using System;
using System.Collections.Generic;
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
        private ModuleDeclaration module;

        private IMethod categoryInitializerMethod;

        private readonly Dictionary<LogLevel, LoggerMethods> loggerMethods = new Dictionary<LogLevel, LoggerMethods>();
        
        private ITypeSignature loggerType;
        
        public void Initialize(ModuleDeclaration module)
        {
            this.module = module;
            this.loggingImplementation = new LoggingImplementationTypeBuilder(module);
            
            this.loggerType = module.FindType(typeof(ILog));
            LoggerMethodsBuilder builder = new LoggerMethodsBuilder(module, this.loggerType);

            this.categoryInitializerMethod = module.FindMethod(module.FindType(typeof(LogManager)), "GetLogger",
                method => method.Parameters.Count == 1 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String));

            this.loggerMethods[LogLevel.Debug] = builder.CreateLoggerMethods("Debug");
            this.loggerMethods[LogLevel.Info] = builder.CreateLoggerMethods("Info");
            this.loggerMethods[LogLevel.Warning] = builder.CreateLoggerMethods("Warn");
            this.loggerMethods[LogLevel.Error] = builder.CreateLoggerMethods("Error");
            this.loggerMethods[LogLevel.Fatal] = builder.CreateLoggerMethods("Fatal");
        }

        public ILoggingBackendInstance CreateInstance(AspectWeaverInstance aspectWeaverInstance)
        {
            return new Log4NetBackendInstance(this);
        }

        private LoggerMethods GetLoggerMethods(LogLevel logLevel)
        {
            return this.loggerMethods[logLevel];
        }

        private class Log4NetBackendInstance : ILoggingBackendInstance
        {
            private readonly Log4NetBackend parent;

            public Log4NetBackendInstance(Log4NetBackend parent)
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

            public void EmitGetIsEnabled(InstructionWriter writer, LogLevel logLevel)
            {
                writer.EmitInstructionField(OpCodeNumber.Ldsfld, this.loggerField);
                LoggerMethods loggerMethods = this.parent.GetLoggerMethods(logLevel);
                writer.EmitInstructionMethod(OpCodeNumber.Callvirt, loggerMethods.IsLoggingEnabledMethod);
            }

            public void EmitWrite(InstructionWriter writer, string messageFormattingString, int argumentsCount, 
                                  LogLevel logLevel, Action<InstructionWriter> getExceptionAction,
                                  Action<int, InstructionWriter> loadArgumentAction, bool useWrapper)
            {
                bool createArgsArray;
                IMethod method = this.GetLoggerMethod(logLevel, argumentsCount, out createArgsArray);

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

                writer.EmitInstructionMethod(method.IsVirtual ? OpCodeNumber.Callvirt : OpCodeNumber.Call, method);
            }

            private IMethod GetLoggerMethod(LogLevel logLevel, int argumentsCount, out bool createArgsArray)
            {
                LoggerMethods loggerMethods = this.parent.GetLoggerMethods(logLevel);
                createArgsArray = false;

                switch (argumentsCount)
                {
                    case 0:
                        return loggerMethods.WriteStringMethod;
                    case 1:
                        return loggerMethods.WriteStringFormat1Method;
                    case 2:
                        return loggerMethods.WriteStringFormat2Method;
                    case 3:
                        return loggerMethods.WriteStringFormat3Method;
                    default:
                        createArgsArray = true;
                        return loggerMethods.WriteStringFormatArrayMethod;
                }
            }
        }
    }
}