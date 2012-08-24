#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using PostSharp.Aspects.Dependencies;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
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
        private readonly Assets assets;

        public LoggingAspectTransformation(LoggingAspectWeaver aspectWeaver, ILoggingBackend backend)
            : base(aspectWeaver)
        {
            this.backend = backend;

            this.assets = aspectWeaver.Module.Cache.GetItem(() => new Assets(aspectWeaver.Module));
        }

        public override string GetDisplayName(MethodSemantics semantic)
        {
            return "Logging Transformation";
        }

        public AspectWeaverTransformationInstance CreateInstance(AspectWeaverInstance aspectWeaverInstance)
        {
            return new LoggingAspectTransformationInstance(this, aspectWeaverInstance);
        }

        private class Assets
        {
            public IMethodSignature ToStringMethodSignature { get; private set; }

            public Assets(ModuleDeclaration module)
            {
                this.ToStringMethodSignature = module.FindMethod(module.Cache.GetType(typeof(object)), "ToString");
            }
        }

        private class LoggingAspectTransformationInstance : MethodBodyTransformationInstance
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
                private readonly ILoggingBackendInstance backendInstance;
                private readonly LoggingAspectTransformationInstance transformationInstance;
                private readonly ConfigurationOptions options;
                private readonly MessageArgumentsFormatter argumentsFormatter;
                private readonly MethodMappingWriter methodMappingWriter;

                public Implementation(LoggingAspectTransformationInstance transformationInstance, MethodBodyTransformationContext context)
                    : base(transformationInstance.AspectWeaver.AspectInfrastructureTask, context)
                {
                    this.transformationInstance = transformationInstance;
                    this.backendInstance = this.transformationInstance.parent.backend.CreateInstance(transformationInstance.AspectWeaverInstance);
                    this.options = new ConfigurationOptions(this.transformationInstance.AspectWeaverInstance);
                    this.argumentsFormatter = new MessageArgumentsFormatter(context);
                    this.methodMappingWriter = context.MethodMapping.CreateWriter();
                }

                public void Implement()
                {
                    ITypeSignature exceptionSignature = this.transformationInstance.AspectWeaver.Module.Cache.GetType(typeof(Exception));

                    bool hasOnEntry = this.options.OnEntryLevel != LogLevel.None;

                    bool hasOnSuccess = this.options.OnSuccessLevel != LogLevel.None;

                    bool hasOnException = this.options.OnExceptionLevel != LogLevel.None;

                    Implement(hasOnEntry, hasOnSuccess, false, hasOnException ? new[] { exceptionSignature } : null);
                    this.Context.AddRedirection(this.Redirection);
                }

                protected override void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer)
                {
                    //MethodDefDeclaration targetMethod = this.transformationInstance.AspectWeaverInstance.TargetElement as MethodDefDeclaration;
                    //if ( targetMethod == null )
                    //{
                    //    return;
                    //}

                    //// TODO: nested types
                    //string category = targetMethod.DeclaringType.Name;
                    //ILoggingCategoryBuilder builder = this.backendInstance.GetCategoryBuilder( category );
                    //InstructionSequence sequence = block.AddInstructionSequence( null, NodePosition.After, null );
                    //writer.AttachInstructionSequence( sequence );

                    //LocalVariableSymbol exceptionLocal = block.MethodBody.RootInstructionBlock.DefineLocalVariable(
                    //    exceptionType, DebuggerSpecialNames.GetVariableSpecialName( "ex" ) );

                    //if ( builder.SupportsIsEnabled )
                    //{
                    //    builder.EmitGetIsEnabled( writer, this.options.OnExceptionLevel );
                    //    InstructionSequence branchSequence = block.AddInstructionSequence( null, NodePosition.After, sequence );
                    //    writer.EmitBranchingInstruction( OpCodeNumber.Brfalse_S, branchSequence );
                    //}

                    //bool useWrapper = this.ShouldUseWrapper( targetMethod );

                    int[] argumentsIndex;
                    string messageFormatString;

                    if (this.options.OnExceptionOptions != LogOptions.None)
                    {
                        string messageArgumentsFormatString = this.argumentsFormatter.CreateMessageArguments(this.options.OnExceptionOptions, out argumentsIndex);
                        messageFormatString = "An exception occurred in " + messageArgumentsFormatString + ":\n{" + argumentsIndex.Length + "}";
                    }
                    else
                    {
                        argumentsIndex = new int[0];
                        messageFormatString = "An exception occurred:\n{0}";
                    }

                    this.EmitMessage(block, writer, this.options.OnExceptionLevel, messageFormatString, argumentsIndex, exceptionType);

                    //builder.EmitWrite( writer, "An exception occurred:\n{0}", 1, this.options.OnExceptionLevel,
                    //                   w => w.EmitInstructionLocalVariable( OpCodeNumber.Stloc, exceptionLocal ),
                    //                   ( i, w ) => w.EmitInstructionLocalVariable( OpCodeNumber.Ldloc, exceptionLocal ), useWrapper );

                    writer.EmitInstruction(OpCodeNumber.Rethrow);
                    writer.DetachInstructionSequence();
                }

                protected override void ImplementOnExit(InstructionBlock block, InstructionWriter writer)
                {
                }

                protected override void ImplementOnEntry(InstructionBlock block, InstructionWriter writer)
                {
                    int[] argumentsIndex;
                    string messageFormatString = this.argumentsFormatter.CreateMessageArguments(this.options.OnEntryOptions, out argumentsIndex);

                    this.EmitMessage(block, writer, this.options.OnEntryLevel, "Entering: " + messageFormatString, argumentsIndex);
                }

                protected override void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer)
                {
                    int[] argumentsIndex;
                    string messageFormatString = this.argumentsFormatter.CreateMessageArguments(this.options.OnSuccessOptions, out argumentsIndex);

                    this.EmitMessage(block, writer, this.options.OnSuccessLevel, "Leaving: " + messageFormatString, argumentsIndex);
                }

                private void EmitMessage(InstructionBlock block, InstructionWriter writer, LogLevel logLevel, string messageFormatString, int[] arguments, ITypeSignature exceptionType = null)
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

                    LocalVariableSymbol exceptionLocal = null;
                    if (exceptionType != null)
                    {
                        exceptionLocal = block.MethodBody.RootInstructionBlock.DefineLocalVariable(
                        exceptionType, DebuggerSpecialNames.GetVariableSpecialName("ex"));
                    }

                    if (builder.SupportsIsEnabled)
                    {
                        builder.EmitGetIsEnabled(writer, logLevel);
                        InstructionSequence branchSequence = block.AddInstructionSequence(null, NodePosition.After, sequence);
                        writer.EmitBranchingInstruction(OpCodeNumber.Brfalse_S, branchSequence);
                    }

                    bool useWrapper = ShouldUseWrapper(targetMethod);

                    Action<InstructionWriter> getExceptionAction = exceptionLocal != null ? (Action<InstructionWriter>)(w => w.EmitInstructionLocalVariable(OpCodeNumber.Stloc, exceptionLocal)) : null;

                    builder.EmitWrite(writer,
                        messageFormatString,
                        exceptionType == null ? arguments.Length : arguments.Length + 1,
                        logLevel,
                        getExceptionAction,
                        (i, instructionWriter) =>
                        {
                            if (i < arguments.Length)
                            {
                                int index = arguments[i];
                                if (index == MessageArgumentsFormatter.ThisArgumentPosition)
                                {
                                    this.methodMappingWriter.EmitLoadInstance(false, instructionWriter);
                                }
                                else if (index == MessageArgumentsFormatter.ReturnParameterPosition)
                                {
                                    instructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, Context.ReturnValueVariable);
                                    instructionWriter.EmitConvertToObject(
                                        Context.MethodMapping.MethodSignature.ReturnType);
                                }
                                else
                                {
                                    this.methodMappingWriter.EmitLoadArgument(index, instructionWriter);

                                    instructionWriter.EmitConvertToObject(this.methodMappingWriter.MethodMapping.MethodSignature.GetParameterType(index));
                                }
                            }
                            else
                            {
                                //Emit exception parameter
                                instructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, exceptionLocal);
                            }
                        },
                        useWrapper);
                    if (exceptionType == null)
                    {
                        writer.DetachInstructionSequence();
                    }
                }

                private bool ShouldUseWrapper(MethodDefDeclaration targetMethod)
                {
                    if (targetMethod.Name == "ToString")
                    {
                        MethodDefDeclaration parent = targetMethod.GetParentDefinition(true);
                        if (parent != null)
                        {
                            if (parent.MatchesReference(
                                this.transformationInstance.parent.assets.ToStringMethodSignature))
                            {
                                return true;
                            }
                        }
                    }

                    for (int i = 0; i < targetMethod.Parameters.Count; i++)
                    {
                        ITypeSignature parameterType = targetMethod.Parameters[i].ParameterType;

                        if (!parameterType.BelongsToClassification(TypeClassifications.Intrinsic) ||
                             IntrinsicTypeSignature.Is(parameterType, IntrinsicType.Object))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }
}