using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Utilities;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    public sealed class LoggingImplementationTypeBuilder
    {
        private readonly Dictionary<string, FieldDefDeclaration> categoryFields;
        private readonly Dictionary<IMethod, MethodDefDeclaration> wrapperMethods;

        private readonly InstructionWriter writer = new InstructionWriter();

        private readonly TypeDefDeclaration implementationType;
        private readonly FieldDefDeclaration isLoggingField;
        private readonly ModuleDeclaration module;
        private readonly WeavingHelper weavingHelper;

        private readonly IMethod stringFormatArrayMethod;
        private readonly IMethod traceWriteLineMethod;

        private InstructionSequence returnSequence;
        private InstructionBlock constructorBlock;

        private IGenericMethodDefinition threadStaticAttributeConstructor;

        public LoggingImplementationTypeBuilder(ModuleDeclaration module)
        {
            this.module = module;
            this.categoryFields = new Dictionary<string, FieldDefDeclaration>();
            this.wrapperMethods = new Dictionary<IMethod, MethodDefDeclaration>();

            this.stringFormatArrayMethod = module.FindMethod(module.Cache.GetIntrinsic(IntrinsicType.String), "Format",
                method => method.Parameters.Count == 2 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                          method.Parameters[1].ParameterType.BelongsToClassification(TypeClassifications.Array));

            this.traceWriteLineMethod = module.FindMethod(module.Cache.GetType(typeof(System.Diagnostics.Trace)), "WriteLine",
                method => method.Parameters.Count == 1 &&
                          IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String));

            this.weavingHelper = new WeavingHelper(module);
            this.implementationType = this.CreateContainingType();
            this.isLoggingField = this.CreateIsLoggingField();
        }

        public FieldDefDeclaration GetCategoryField(string category, ITypeSignature fieldType, Action<InstructionWriter> initializeFieldAction)
        {
            FieldDefDeclaration categoryField;
            if (!this.categoryFields.TryGetValue(category, out categoryField))
            {
                categoryField = this.CreateCategoryField(fieldType, initializeFieldAction);
                this.categoryFields[category] = categoryField;
            }

            return categoryField;
        }

        public MethodDefDeclaration GetTraceStringFormatMethod(string prefix)
        {
            string wrapperName = string.Format("{0}{1}Format", prefix, this.traceWriteLineMethod.Name);
            MethodDefDeclaration wrapperMethod = this.implementationType.Methods.GetOneByName(wrapperName) ??
                                                 this.CreateTraceStringFormatWrapper(wrapperName);

            return wrapperMethod;
        }

        private MethodDefDeclaration CreateTraceStringFormatWrapper(string name)
        {
            MethodDefDeclaration formatWrapperMethod = new MethodDefDeclaration
            {
                Name = name,
                Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            };
            this.implementationType.Methods.Add(formatWrapperMethod);

            formatWrapperMethod.Parameters.Add(new ParameterDeclaration(0, "format", this.module.Cache.GetIntrinsic(IntrinsicType.String)));
            formatWrapperMethod.Parameters.Add(new ParameterDeclaration(1, "args", this.module.Cache.GetType(typeof(object[]))));

            InstructionBlock block = formatWrapperMethod.MethodBody.CreateInstructionBlock();
            formatWrapperMethod.MethodBody.RootInstructionBlock = block;
            InstructionSequence sequence = block.AddInstructionSequence(null, NodePosition.After, null);
            this.writer.AttachInstructionSequence(sequence);
            
            for (int i = 0; i < formatWrapperMethod.Parameters.Count; i++)
            {
                this.writer.EmitInstructionInt16(OpCodeNumber.Ldarg, (short)i);
            }
            
            this.writer.EmitInstructionMethod(OpCodeNumber.Call, this.stringFormatArrayMethod);

            this.EmitCallHandler(this.traceWriteLineMethod);
            this.writer.EmitInstruction(OpCodeNumber.Ret);
            this.writer.DetachInstructionSequence();

            return formatWrapperMethod;
        }

        private void EmitCallHandler(IMethod loggerMethod)
        {
            this.writer.EmitInstructionMethod(
                !loggerMethod.IsVirtual || (loggerMethod.IsSealed || loggerMethod.DeclaringType.IsSealed)
                    ? OpCodeNumber.Call
                    : OpCodeNumber.Callvirt,
                loggerMethod.TranslateMethod(this.module));
        }

        public MethodDefDeclaration GetWriteWrapperMethod(string name, IMethod loggerMethod)
        {
            MethodDefDeclaration wrapperMethod;
            if (!this.wrapperMethods.TryGetValue(loggerMethod, out wrapperMethod))
            {
                IMethod targetMethod = loggerMethod;
                
                wrapperMethod = this.CreateWrapperMethod(name, targetMethod);
                this.wrapperMethods[loggerMethod] = wrapperMethod;
            }

            return wrapperMethod;
        }

        private MethodDefDeclaration CreateWrapperMethod(string methodName, IMethod loggerMethod)
        {
            MethodDefDeclaration wrapperMethod = new MethodDefDeclaration
            {
                Name = methodName,
                Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            };
            this.implementationType.Methods.Add(wrapperMethod);

            wrapperMethod.ReturnParameter = new ParameterDeclaration
            {
                Attributes = ParameterAttributes.Retval,
                ParameterType = this.module.Cache.GetIntrinsic(IntrinsicType.Void)
            };

            ITypeSignature loggerFieldType = loggerMethod.DeclaringType.GetNakedType();
            MethodDefDeclaration loggerMethodDefinition = loggerMethod.GetMethodDefinition();
            if (!loggerMethodDefinition.IsStatic)
            {
                wrapperMethod.Parameters.Add(new ParameterDeclaration(0, "logger", loggerFieldType));
            }

            for (int i = 0; i < loggerMethodDefinition.Parameters.Count; i++)
            {
                ParameterDeclaration parameter = loggerMethodDefinition.Parameters[i];
                wrapperMethod.Parameters.Add(new ParameterDeclaration(wrapperMethod.Parameters.Count, parameter.Name, parameter.ParameterType.TranslateType(this.module)));
            }

            this.EmitWrapperCallBody(wrapperMethod, loggerMethod);

            return wrapperMethod;
        }

        private void EmitWrapperCallBody(MethodDefDeclaration wrapperMethod, IMethod loggerMethod)
        {
            InstructionBlock rootBlock = wrapperMethod.MethodBody.CreateInstructionBlock();
            wrapperMethod.MethodBody.RootInstructionBlock = rootBlock;

            InstructionBlock parentBlock = rootBlock.AddChildBlock(null, NodePosition.After, null);
            InstructionBlock tryBlock = rootBlock.AddChildBlock(null, NodePosition.After, null);
            InstructionBlock leaveTryBlock = rootBlock.AddChildBlock(null, NodePosition.After, null);
            
            InstructionSequence sequence = parentBlock.AddInstructionSequence(null, NodePosition.After, null);
            // set isLogging to true
            this.writer.AttachInstructionSequence(sequence);
            this.writer.EmitInstruction(OpCodeNumber.Ldc_I4_1);
            this.writer.EmitInstructionField(OpCodeNumber.Stsfld, this.isLoggingField);
            this.writer.DetachInstructionSequence();

            // if isLogging is true, return
            InstructionSequence branchSequence = parentBlock.AddInstructionSequence(null, NodePosition.Before, null);
            this.writer.AttachInstructionSequence(branchSequence);
            this.writer.EmitInstructionField(OpCodeNumber.Ldsfld, this.isLoggingField);
            this.writer.EmitBranchingInstruction(OpCodeNumber.Brfalse_S, sequence);
            this.writer.EmitInstruction(OpCodeNumber.Ret);
            this.writer.DetachInstructionSequence();

            // return instruction at the end of the method
            InstructionSequence retSequence = leaveTryBlock.AddInstructionSequence(null, NodePosition.After, null);
            this.writer.AttachInstructionSequence(retSequence);
            this.writer.EmitInstruction(OpCodeNumber.Ret);
            this.writer.DetachInstructionSequence();

            InstructionSequence trySequence = tryBlock.AddInstructionSequence(null, NodePosition.After, null);
            this.writer.AttachInstructionSequence(trySequence);

            for (int i = 0; i < wrapperMethod.Parameters.Count; i++)
            {
                writer.EmitInstructionInt16(OpCodeNumber.Ldarg, (short)i);
            }

            this.EmitCallHandler(loggerMethod);

            this.writer.DetachInstructionSequence();

            InstructionSequence leaveSequence = tryBlock.AddInstructionSequence(null, NodePosition.After, null);
            this.writer.AttachInstructionSequence(leaveSequence);
            this.writer.EmitBranchingInstruction(OpCodeNumber.Leave, retSequence);
            this.writer.DetachInstructionSequence();

            InstructionBlock protectedBlock;
            InstructionBlock[] catchBlocks;
            InstructionBlock finallyBlock;
            this.weavingHelper.AddExceptionHandlers(this.writer, tryBlock, leaveSequence, null, true, out protectedBlock, out catchBlocks, out finallyBlock);

            InstructionSequence finallySequence = finallyBlock.AddInstructionSequence(null, NodePosition.After, null);
            this.writer.AttachInstructionSequence(finallySequence);
            this.writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            this.writer.EmitInstructionField(OpCodeNumber.Stsfld, this.isLoggingField);
            //this.writer.EmitInstruction(OpCodeNumber.Endfinally);
            this.writer.DetachInstructionSequence();
        }

        private void EmitContstructorBlock(MethodDefDeclaration staticConstructor)
        {
            this.constructorBlock = staticConstructor.MethodBody.RootInstructionBlock = staticConstructor.MethodBody.CreateInstructionBlock();
            this.returnSequence = staticConstructor.MethodBody.RootInstructionBlock.AddInstructionSequence(null,
                                                                                                           NodePosition.After,
                                                                                                           null);
            this.writer.AttachInstructionSequence(this.returnSequence);
            this.writer.EmitInstruction(OpCodeNumber.Ret);
            this.writer.DetachInstructionSequence();
        }

        private void EmitStringFormatOverload()
        {

        }

        private FieldDefDeclaration CreateIsLoggingField()
        {
            FieldDefDeclaration isLoggingFieldDef = new FieldDefDeclaration
            {
                Name = "isLogging",
                Attributes = FieldAttributes.Private | FieldAttributes.Static,
                FieldType = this.module.Cache.GetType(typeof(bool))
            };
            this.implementationType.Fields.Add(isLoggingFieldDef);

            isLoggingFieldDef.CustomAttributes.Add(this.CreateThreadStaticAttribute());

            return isLoggingFieldDef;
        }

        private TypeDefDeclaration CreateContainingType()
        {
            string uniqueName = this.module.Types.GetUniqueName(
                DebuggerSpecialNames.GetDeclarationSpecialName("LoggingImplementationDetails{0}"));

            TypeDefDeclaration logCategoriesType = new TypeDefDeclaration
            {
                Name = uniqueName,
                Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Abstract,
                BaseType = ((IType)this.module.Cache.GetType("System.Object, mscorlib"))
            };
            this.module.Types.Add(logCategoriesType);

            // Add [CompilerGenerated] and [DebuggerNonUserCode] to the type
            this.weavingHelper.AddCompilerGeneratedAttribute(logCategoriesType.CustomAttributes);
            this.weavingHelper.AddDebuggerNonUserCodeAttribute(logCategoriesType.CustomAttributes);

            MethodDefDeclaration staticConstructor = new MethodDefDeclaration
            {
                Name = ".cctor",
                Attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.RTSpecialName |
                             MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            };
            logCategoriesType.Methods.Add(staticConstructor);

            staticConstructor.ReturnParameter = new ParameterDeclaration
            {
                Attributes = ParameterAttributes.Retval,
                ParameterType = this.module.Cache.GetIntrinsic(IntrinsicType.Void)
            };

            this.EmitContstructorBlock(staticConstructor);

            return logCategoriesType;
        }

        private FieldDefDeclaration CreateCategoryField(ITypeSignature fieldType, Action<InstructionWriter> initializeFieldAction)
        {
            string fieldName = string.Format("l{0}", this.implementationType.Fields.Count);

            FieldDefDeclaration loggerFieldDef = new FieldDefDeclaration
            {
                Name = fieldName,
                Attributes = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly,
                FieldType = fieldType
            };
            this.implementationType.Fields.Add(loggerFieldDef);

            InstructionSequence sequence = this.constructorBlock.AddInstructionSequence(null,
                                                                                        NodePosition.Before,
                                                                                        this.returnSequence);

            this.writer.AttachInstructionSequence(sequence);
            initializeFieldAction(this.writer);
            this.writer.EmitInstructionField(OpCodeNumber.Stsfld, loggerFieldDef);
            this.writer.DetachInstructionSequence();

            return loggerFieldDef;
        }

        private CustomAttributeDeclaration CreateThreadStaticAttribute()
        {
            if ( this.threadStaticAttributeConstructor == null )
            {
                this.threadStaticAttributeConstructor = this.module.FindMethod( "System.ThreadStaticAttribute, mscorlib",
                                                                                ".ctor",
                                                                                BindingOptions.DontThrowException, 0 );
            }

            return new CustomAttributeDeclaration( this.threadStaticAttributeConstructor );
        }
    }
}