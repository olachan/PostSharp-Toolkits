using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Reflection;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Binding;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Utilities;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    public sealed class LoggingImplementationTypeBuilder
    {
        private readonly Dictionary<string, FieldDefDeclaration> categoryFields;
        private readonly Dictionary<IMethodSignature, MethodDefDeclaration> wrapperMethods;
        private readonly InstructionWriter writer = new InstructionWriter();
        private readonly TypeDefDeclaration containingType;
        private readonly FieldDefDeclaration isLoggingField;
        private readonly ModuleDeclaration module;
        private readonly WeavingHelper weavingHelper;

        private InstructionSequence returnSequence;
        private InstructionBlock constructorBlock;
        private IGenericMethodDefinition threadStaticAttributeConstructor;

        public LoggingImplementationTypeBuilder(ModuleDeclaration module)
        {
            this.categoryFields = new Dictionary<string, FieldDefDeclaration>();
            this.wrapperMethods = new Dictionary<IMethodSignature, MethodDefDeclaration>(new MethodSignatureComparer(BindingOptions.BindingEqualityOptions));


            this.module = module;
            this.weavingHelper = new WeavingHelper(module);
            this.containingType = this.CreateContainingType();
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

        public MethodDefDeclaration GetWriteWrapperMethod(IMethodSignature loggerMethod, ITypeSignature loggerType)
        {
            MethodDefDeclaration wrapperMethod;
            if (!this.wrapperMethods.TryGetValue(loggerMethod, out wrapperMethod))
            {
                wrapperMethod = this.CreateWrapperMethod(loggerMethod, loggerType);
                this.wrapperMethods[loggerMethod] = wrapperMethod;
            }

            return wrapperMethod;
        }

        public MethodDefDeclaration CreateWrapperMethod(IMethodSignature loggerMethod, ITypeSignature loggerFieldType)
        {
            MethodDefDeclaration wrapperMethod = new MethodDefDeclaration
            {
                Name = "Write",
                Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            };
            this.containingType.Methods.Add(wrapperMethod);

            wrapperMethod.ReturnParameter = new ParameterDeclaration
            {
                Attributes = ParameterAttributes.Retval,
                ParameterType = this.module.Cache.GetIntrinsic(IntrinsicType.Void)
            };

            wrapperMethod.Parameters.Add(new ParameterDeclaration(0, "logger", loggerFieldType));

            for (int i = 0; i < loggerMethod.ParameterCount; i++)
            {
                wrapperMethod.Parameters.Add(new ParameterDeclaration(i + 1, "arg" + i, loggerMethod.GetParameterType(i).TranslateType(this.module)));
            }

            this.EmitWrapperCallBody(wrapperMethod);

            return wrapperMethod;
        }

        public void f()
        {
            //InstructionBlock rootBlock = wrapperMethod.MethodBody.CreateInstructionBlock();
            //wrapperMethod.MethodBody.RootInstructionBlock = rootBlock;

            //InstructionBlock leaveBlock = rootBlock.AddChildBlock(null, NodePosition.After, null);
            //InstructionBlock tryBlock = rootBlock.AddChildBlock(null, NodePosition.After, null);
            //InstructionBlock finallyBlock = rootBlock.AddChildBlock(null, NodePosition.After, tryBlock);

            //InstructionSequence sequence = rootBlock.AddInstructionSequence(null, NodePosition.Before, null);
            //this.writer.AttachInstructionSequence(sequence);

            //this.writer.EmitInstruction(OpCodeNumber.Ldc_I4_1);
            //this.writer.EmitInstructionField(OpCodeNumber.Stsfld, this.isLoggingField);

            //this.writer.DetachInstructionSequence();

            //InstructionSequence leaveSequence = leaveBlock.AddInstructionSequence(null, NodePosition.After, null);
            //this.writer.AttachInstructionSequence(leaveSequence);
            //this.writer.EmitInstruction(OpCodeNumber.Ret);
            //this.writer.DetachInstructionSequence();

            //InstructionSequence trySequence = tryBlock.AddInstructionSequence(null, NodePosition.After, leaveSequence);

            //this.writer.AttachInstructionSequence(trySequence);
            //this.writer.EmitBranchingInstruction(OpCodeNumber.Leave, trySequence);

            //this.writer.DetachInstructionSequence();

            //InstructionSequence finallySequence = finallyBlock.AddInstructionSequence(null, NodePosition.After, null);
            //this.writer.AttachInstructionSequence(finallySequence);

            //this.writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            //this.writer.EmitInstructionField(OpCodeNumber.Stsfld, this.isLoggingField);

            //this.writer.DetachInstructionSequence();

        }

        private void EmitWrapperCallBody(MethodDefDeclaration wrapperMethod)
        {
            InstructionBlock rootBlock = wrapperMethod.MethodBody.CreateInstructionBlock();
            wrapperMethod.MethodBody.RootInstructionBlock = rootBlock;

            InstructionBlock parentBlock = rootBlock.AddChildBlock(null, NodePosition.After, null);
            InstructionBlock tryBlock = rootBlock.AddChildBlock(null, NodePosition.After, null);
            InstructionBlock leaveTryBlock = rootBlock.AddChildBlock(null, NodePosition.After, null);
            
            InstructionSequence sequence = parentBlock.AddInstructionSequence(null, NodePosition.After, null);
            InstructionSequence branchSequence = parentBlock.AddInstructionSequence(null, NodePosition.Before, null);

            // if isLogging is true, return
            this.writer.AttachInstructionSequence(branchSequence);
            this.writer.EmitInstructionField(OpCodeNumber.Ldsfld, this.isLoggingField);
            this.writer.EmitBranchingInstruction(OpCodeNumber.Brfalse_S, sequence);
            this.writer.EmitInstruction(OpCodeNumber.Ret);
            this.writer.DetachInstructionSequence();

            InstructionSequence leaveTrySequence = leaveTryBlock.AddInstructionSequence(null, NodePosition.After, null);
            this.writer.AttachInstructionSequence(leaveTrySequence);
            this.writer.EmitInstruction(OpCodeNumber.Ret);
            this.writer.DetachInstructionSequence();

            InstructionSequence leaveSequence = tryBlock.AddInstructionSequence(null, NodePosition.After, null);
            this.writer.AttachInstructionSequence(leaveSequence);
            this.writer.EmitBranchingInstruction(OpCodeNumber.Leave, leaveTrySequence);
            this.writer.DetachInstructionSequence();

            this.writer.AttachInstructionSequence(sequence);

            // set isLogging to true
            this.writer.EmitInstruction(OpCodeNumber.Ldc_I4_1);
            this.writer.EmitInstructionField(OpCodeNumber.Stsfld, this.isLoggingField);

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

        private FieldDefDeclaration CreateIsLoggingField()
        {
            FieldDefDeclaration isLoggingFieldDef = new FieldDefDeclaration
            {
                Name = "isLogging",
                Attributes = FieldAttributes.Private | FieldAttributes.Static,
                FieldType = this.module.Cache.GetType(typeof(bool))
            };
            this.containingType.Fields.Add(isLoggingFieldDef);

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
            string fieldName = string.Format("l{0}", this.containingType.Fields.Count);

            FieldDefDeclaration loggerFieldDef = new FieldDefDeclaration
            {
                Name = fieldName,
                Attributes = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly,
                FieldType = fieldType
            };
            this.containingType.Fields.Add(loggerFieldDef);

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
    }
}