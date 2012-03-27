using System;
using System.Text;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Utilities;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    internal sealed class LoggingAspectTransformation : MethodBodyTransformation
    {
        private readonly ILoggingBackend backend;

        public LoggingAspectTransformation(LoggingAspectWeaver aspectWeaver, ILoggingBackend backend)
            : base(aspectWeaver)
        {
            this.backend = backend;
        }

        public override string GetDisplayName(MethodSemantics semantic)
        {
            return "Logging Transformation";
        }

        public AspectWeaverTransformationInstance CreateInstance(AspectWeaverInstance aspectWeaverInstance)
        {
            return new LoggingAspectTransformationInstance(this, aspectWeaverInstance);
        }

        public class LoggingAspectTransformationInstance : MethodBodyTransformationInstance
        {
            private readonly LoggingAspectTransformation parent;

            public LoggingAspectTransformationInstance(LoggingAspectTransformation parent, AspectWeaverInstance aspectWeaverInstance)
                : base(parent, aspectWeaverInstance)
            {
                this.parent = parent;
            }

            public override void Implement(MethodBodyTransformationContext context)
            {
                Implementation implementation = new Implementation(this, context);
                implementation.Implement();
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic)
            {
                return MethodBodyTransformationOptions.None;
            }

            private sealed class Implementation : MethodBodyWrappingImplementation
            {
                private readonly LoggingAspectTransformationInstance transformationInstance;
                private readonly ILoggingBackendInstance backendInstance;
                private readonly LogOptions onEntryOptions;
                private readonly LogOptions onSuccessOptions;
                private readonly LogOptions onExceptionOptions;
                private readonly LogLevel onEntryLevel;
                private readonly LogLevel onSuccessLevel;
                private readonly LogLevel onExceptionLevel;
                
                public Implementation(LoggingAspectTransformationInstance transformationInstance, MethodBodyTransformationContext context)
                    : base(transformationInstance.AspectWeaver.AspectInfrastructureTask, context)
                {
                    this.transformationInstance = transformationInstance;
                    this.backendInstance = this.transformationInstance.parent.backend.CreateInstance(transformationInstance.AspectWeaverInstance);

                    AspectWeaverInstance aspectWeaverInstance = this.transformationInstance.AspectWeaverInstance;
                    this.onEntryOptions = aspectWeaverInstance.GetConfigurationValue<LogAspectConfiguration, LogOptions>(c => c.OnEntryOptions);
                    this.onSuccessOptions = aspectWeaverInstance.GetConfigurationValue<LogAspectConfiguration, LogOptions>(c => c.OnSuccessOptions);
                    this.onExceptionOptions = aspectWeaverInstance.GetConfigurationValue<LogAspectConfiguration, LogOptions>(c => c.OnExceptionOptions);
                    this.onEntryLevel = aspectWeaverInstance.GetConfigurationValue<LogAspectConfiguration, LogLevel>(c => c.OnEntryLevel);
                    this.onSuccessLevel = aspectWeaverInstance.GetConfigurationValue<LogAspectConfiguration, LogLevel>(c => c.OnSuccessLevel);
                    this.onExceptionLevel = aspectWeaverInstance.GetConfigurationValue<LogAspectConfiguration, LogLevel>(c => c.OnExceptionLevel);
                }

                public void Implement()
                {
                    ITypeSignature exceptionSignature = this.transformationInstance.AspectWeaver.Module.Cache.GetType(typeof(Exception));

                    bool hasOnEntry = this.onEntryLevel != LogLevel.None;

                    bool hasOnSuccess = this.onSuccessLevel != LogLevel.None;

                    bool hasOnException = this.onExceptionLevel != LogLevel.None;

                    Implement(hasOnEntry, hasOnSuccess, false, hasOnException ? new[] { exceptionSignature } : null);
                    this.Context.AddRedirection(this.Redirection);
                }

                protected override void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer)
                {
                    MethodDefDeclaration targetMethod = this.transformationInstance.AspectWeaverInstance.TargetElement as MethodDefDeclaration;
                    if (targetMethod == null)
                    {
                        return;
                    }

                    // TODO: nested types
                    string category = targetMethod.DeclaringType.Name;
                    ILoggingCategoryBuilder builder = this.backendInstance.GetCategoryBuilder(category);
                    InstructionSequence sequence = block.AddInstructionSequence(null, NodePosition.After, null);
                    writer.AttachInstructionSequence(sequence);

                    LocalVariableSymbol exceptionLocal = block.MethodBody.RootInstructionBlock.DefineLocalVariable(
                        exceptionType, DebuggerSpecialNames.GetVariableSpecialName("ex"));

                    if (builder.SupportsIsEnabled)
                    {
                        builder.EmitGetIsEnabled(writer, this.onExceptionLevel);
                        InstructionSequence branchSequence = block.AddInstructionSequence(null, NodePosition.After, sequence);
                        writer.EmitBranchingInstruction(OpCodeNumber.Brfalse_S, branchSequence);
                    }

                    bool useWrapper = this.ShouldUseWrapper(Context);

                    builder.EmitWrite(writer, "An exception occurred:\n{0}", 1, this.onExceptionLevel,
                                      w => w.EmitInstructionLocalVariable(OpCodeNumber.Stloc, exceptionLocal),
                                      (i, w) => w.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, exceptionLocal), useWrapper);

                    writer.EmitInstruction(OpCodeNumber.Rethrow);
                    writer.DetachInstructionSequence();
                }

                protected override void ImplementOnExit(InstructionBlock block, InstructionWriter writer) { }

                protected override void ImplementOnEntry(InstructionBlock block, InstructionWriter writer)
                {
                    string messageFormatString = this.CreateMessageFormatString(this.onEntryOptions);

                    this.EmitMessage(block, writer, this.onEntryLevel, this.onEntryOptions, "Entering: " + messageFormatString);
                }

                protected override void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer)
                {
                    string messageFormatString = this.CreateMessageFormatString(this.onSuccessOptions);

                    this.EmitMessage(block, writer, this.onSuccessLevel, this.onSuccessOptions, "Leaving: " + messageFormatString);
                }

                private void EmitMessage(InstructionBlock block, InstructionWriter writer, LogLevel logLevel, LogOptions logOptions, string messageFormatString)
                {
                    MethodDefDeclaration targetMethod = Context.TargetElement as MethodDefDeclaration;
                    if (targetMethod == null)
                    {
                        return;
                    }

                    // TODO: nested types
                    string category = targetMethod.DeclaringType.Name;
                    ILoggingCategoryBuilder builder = this.backendInstance.GetCategoryBuilder(category);

                    InstructionSequence sequence = block.AddInstructionSequence(null, NodePosition.After, null);
                    writer.AttachInstructionSequence(sequence);
                    
                    if (builder.SupportsIsEnabled)
                    {
                        builder.EmitGetIsEnabled(writer, logLevel);
                        InstructionSequence branchSequence = block.AddInstructionSequence(null, NodePosition.After, sequence);
                        writer.EmitBranchingInstruction(OpCodeNumber.Brfalse_S, branchSequence);
                    }

                    
                    bool hasThis = Context.MethodMapping.MethodSignature.CallingConvention == CallingConvention.HasThis;

                    int parameterCount = Context.MethodMapping.MethodSignature.ParameterCount;
                    
                    int startArgument = 0;
                    bool shouldLogThisArgument = ((logOptions & LogOptions.IncludeThisArgument) != 0 && hasThis);
                    if (shouldLogThisArgument)
                    {
                        parameterCount++;
                        startArgument = 1;
                    }

                    bool useWrapper = ShouldUseWrapper(Context);

                    builder.EmitWrite(writer, messageFormatString, parameterCount, logLevel, null, (i, instructionWriter) =>
                    {
                        if (shouldLogThisArgument && i == 0)
                        {
                            instructionWriter.EmitInstruction(OpCodeNumber.Ldarg_0);
                        }
                        else
                        {
                            instructionWriter.EmitInstructionInt16(OpCodeNumber.Ldarg, (short)(hasThis ? i + 1 : i));

                            instructionWriter.EmitConvertToObject(Context.MethodMapping.MethodSignature.GetParameterType(shouldLogThisArgument ? i - startArgument : i));
                        }
                    },
                    
                    useWrapper);
                    
                    writer.DetachInstructionSequence();
                }

                private bool ShouldUseWrapper(MethodBodyTransformationContext context)
                {
                    MethodDefDeclaration methodDef = context.TargetElement as MethodDefDeclaration;
                    if (methodDef != null)
                    {
                        if (methodDef.Name == "ToString")
                        {
                            return true;
                        }
                    }

                    for (int i = 0; i < context.MethodMapping.MethodSignature.ParameterCount; i++)
                    {
                        ITypeSignature parameterType = context.MethodMapping.MethodSignature.GetParameterType(i);

                        if (!parameterType.BelongsToClassification(TypeClassifications.Intrinsic) ||
                            IntrinsicTypeSignature.Is(parameterType, IntrinsicType.Object))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                private string CreateMessageFormatString(LogOptions logOptions)
                {
                    MethodBodyTransformationContext context1 = Context;
                    StringBuilder formatBuilder = new StringBuilder();

                    MethodDefDeclaration targetMethod = context1.TargetElement as MethodDefDeclaration;
                    if (targetMethod == null)
                    {
                        return null;
                    }

                    formatBuilder.AppendFormat("{0}.{1}", targetMethod.DeclaringType, targetMethod.Name);
                    formatBuilder.Append("(");

                    int startParameter = 0;
                    if ((logOptions & LogOptions.IncludeThisArgument) != 0)
                    {
                        if (context1.MethodMapping.MethodSignature.CallingConvention == CallingConvention.HasThis)
                        {
                            formatBuilder.AppendFormat("this = ");
                            AppendFormatPlaceholder(0, formatBuilder, targetMethod.DeclaringType);
                            startParameter = 1;
                        }
                    }

                    int parameterCount = context1.MethodMapping.MethodSignature.ParameterCount;
                    for (int i = 0; i < parameterCount; i++)
                    {
                        if (i > 0)
                        {
                            formatBuilder.Append(", ");
                        }

                        ITypeSignature parameterType = context1.MethodMapping.MethodSignature.GetParameterType(i);
                        if ((logOptions & LogOptions.IncludeParameterType) != 0)
                        {
                            formatBuilder.Append(parameterType.ToString());
                            formatBuilder.Append(' ');
                        }

                        if ((logOptions & LogOptions.IncludeParameterName) != 0)
                        {
                            formatBuilder.Append(context1.MethodMapping.MethodMappingInformation.GetParameterName(i));
                            formatBuilder.Append(' ');
                        }

                        if ((logOptions & LogOptions.IncludeParameterValue) != 0)
                        {
                            formatBuilder.AppendFormat("= ");

                            AppendFormatPlaceholder(i + startParameter, formatBuilder, parameterType);
                        }
                    }
            
                    formatBuilder.Append(")");

                    return formatBuilder.ToString();
                }
            }

            private static void AppendFormatPlaceholder(int i, StringBuilder formatBuilder, ITypeSignature parameterType)
            {
                if (IntrinsicTypeSignature.Is(parameterType, IntrinsicType.String))
                {
                    formatBuilder.AppendFormat("\"" + "{{{0}}}" + "\"", i);
                }
                else
                {
                    formatBuilder.AppendFormat("{{{0}}}", i);
                }
            }
        }
    }
}