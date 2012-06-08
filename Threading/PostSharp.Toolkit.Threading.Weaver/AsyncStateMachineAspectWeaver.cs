#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.AspectWeavers;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;

namespace PostSharp.Toolkit.Threading.Weaver
{
    internal sealed class AsyncStateMachineAspectWeaver : TypeLevelAspectWeaver
    {
        private MoveNextTransformation moveNextTransformation;

        public AsyncStateMachineAspectWeaver() : base( new AspectConfigurationAttribute(), MulticastTargets.Struct )
        {
           
        }

        protected override AspectWeaverInstance CreateAspectWeaverInstance( AspectInstanceInfo aspectInstanceInfo )
        {
            return new AsyncStateMachineAspectWeaverInstance( this, aspectInstanceInfo );
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.moveNextTransformation = new MoveNextTransformation(this);
        }

        private sealed class AsyncStateMachineAspectWeaverInstance : TypeLevelAspectWeaverInstance
        {
            private readonly AspectInstanceInfo aspectInstanceInfo;
            private readonly AsyncStateMachineAspectWeaver parent;

            public AsyncStateMachineAspectWeaverInstance( AsyncStateMachineAspectWeaver parent, AspectInstanceInfo aspectInstanceInfo ) :
                base( parent, aspectInstanceInfo )
            {
                this.parent = parent;
                this.aspectInstanceInfo = aspectInstanceInfo;
            }

            public override void ProvideAspectTransformations( AspectWeaverTransformationAdder adder )
            {
                // Find the MoveNext method.
                TypeDefDeclaration targetType = (TypeDefDeclaration) this.aspectInstanceInfo.TargetElement;
                MethodDefDeclaration moveNextMethod = targetType.Methods.Single<MethodDefDeclaration>( m => m.Name.EndsWith( "MoveNext" ) );

                adder.Add( moveNextMethod, this.parent.moveNextTransformation.CreateInstance( this ) );
            }
        }


        private sealed class MoveNextTransformation : MethodBodyTransformation
        {
            // ReSharper disable InconsistentNaming

            private readonly IMethod actor_GetDispatcher_Method;
            private readonly IMethod dispatcher_CheckAccess_Method;
            private readonly IMethod dispatcher_getSynchronizationContext_Method;
            private readonly IMethod task_Yield_Method;
            private readonly IMethod yieldAwaitable_GetAwaiter_Method;
            private readonly IType yieldAwaiter_Type;
            private readonly IGenericMethodDefinition asyncVoidMethodBuilder_AwaitUnsafeOnCompleted_Method;
            private readonly IGenericMethodDefinition asyncTaskMethodBuilder_AwaitUnsafeOnCompleted_Method;
            private readonly IGenericMethodDefinition asyncTaskMethodBuilderGeneric_AwaitUnsafeOnCompleted_Method;
            private readonly IType asyncVoidMethodBuilder_Type;
            private readonly IType asyncTaskMethodBuilder_Type;
            private readonly IType asyncTaskMethodBuilderGenericType;
            private readonly IType synchronizationContext_Type;
            private readonly IMethod synchronizationContext_getCurrent_Method;
            private readonly IMethod synchronizationContext_SetSynchronizationContext_Method;
            // ReSharper restore InconsistentNaming

            public MoveNextTransformation( AsyncStateMachineAspectWeaver parent )
                : base( parent )
            {
                ModuleDeclaration module = parent.Module;

                // ReSharper disable InconsistentNaming
                IType actor_Type = (IType) module.FindType( typeof(Actor), BindingOptions.Default );
                this.actor_GetDispatcher_Method = module.FindMethod( actor_Type, "get_Dispatcher" );
                

                IType dispatcher_Type = (IType) module.FindType( typeof(IDispatcher), BindingOptions.Default );
                this.dispatcher_CheckAccess_Method = module.FindMethod( dispatcher_Type, "CheckAccess" );
                this.dispatcher_getSynchronizationContext_Method = module.FindMethod( dispatcher_Type, "get_SynchronizationContext" );

                IType task_Type = (IType) module.FindType( typeof(Task), BindingOptions.Default );
                this.task_Yield_Method = module.FindMethod( task_Type, "Yield", 0 );

                IType yieldAwaitable_Type = (IType) module.FindType( "System.Runtime.CompilerServices.YieldAwaitable, mscorlib", BindingOptions.Default );
                this.yieldAwaitable_GetAwaiter_Method = module.FindMethod( yieldAwaitable_Type, "GetAwaiter" );

                this.yieldAwaiter_Type = (IType) this.yieldAwaitable_GetAwaiter_Method.ReturnType;

                this.asyncVoidMethodBuilder_Type = (IType) module.FindType( "System.Runtime.CompilerServices.AsyncVoidMethodBuilder, mscorlib",
                                                                            BindingOptions.Default );
                this.asyncVoidMethodBuilder_AwaitUnsafeOnCompleted_Method = module.FindMethod( this.asyncVoidMethodBuilder_Type, "AwaitUnsafeOnCompleted" );

                this.asyncTaskMethodBuilder_Type = (IType)module.FindType("System.Runtime.CompilerServices.AsyncTaskMethodBuilder, mscorlib",
                                                                            BindingOptions.Default );
                this.asyncTaskMethodBuilder_AwaitUnsafeOnCompleted_Method = module.FindMethod( this.asyncTaskMethodBuilder_Type, "AwaitUnsafeOnCompleted" );

                this.asyncTaskMethodBuilderGenericType = (IType)module.FindType("System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1, mscorlib",
                                                                                  BindingOptions.Default );
                this.asyncTaskMethodBuilderGeneric_AwaitUnsafeOnCompleted_Method = module.FindMethod( this.asyncTaskMethodBuilderGenericType,
                                                                                                      "AwaitUnsafeOnCompleted" );

                this.synchronizationContext_Type = (IType) module.FindType( typeof(SynchronizationContext), BindingOptions.Default );
                this.synchronizationContext_getCurrent_Method = module.FindMethod( this.synchronizationContext_Type, "get_Current" );
                this.synchronizationContext_SetSynchronizationContext_Method = module.FindMethod(this.synchronizationContext_Type, "SetSynchronizationContext");


                // ReSharper restore InconsistentNaming
            }

            
            public AspectWeaverTransformationInstance CreateInstance( AsyncStateMachineAspectWeaverInstance aspectWeaverInstance )
            {
                return new Instance( this, aspectWeaverInstance );
            }

            private sealed class Instance : MethodBodyTransformationInstance
            {
                private readonly MoveNextTransformation parent;

                public Instance( MoveNextTransformation parent, AsyncStateMachineAspectWeaverInstance aspectWeaverInstance )
                    : base( parent, aspectWeaverInstance )
                {
                    this.parent = parent;
                }

                public override void Implement( MethodBodyTransformationContext context )
                {


                    /* We want to generate the following
                     *  if ( !this.<>4__this.Dispatcher.CheckAccess() )
                     *   {
                     *      SynchronizationContext oldContext = SynchronizationContext.Current;
                     *      SynchronizationContext.SetSynchronizationContext( this.<>4__this.Dispatcher.SynchronizationContext );
                     *      this.<>t__dispatchAwaiter = Task.Yield().GetAwaiter();
                     *      this.<>t__builder.AwaitUnsafeOnCompleted<YieldAwaitable.YieldAwaiter, Player.<Ping>d__2>(ref  this.<>t__dispatchAwaiter, ref this);
                     *      SynchronizationContext.SetSynchronizationContext( oldContext );
                     *      return;
                     *   }
                     * 
                     */



                    MethodDefDeclaration targetMethod = (MethodDefDeclaration) context.TargetElement;
                    TypeDefDeclaration targetType = targetMethod.DeclaringType;

                    targetMethod.MethodBody.MaxStack = -1;
                 
                    // Add the field where we will store the awaiter.
                    FieldDefDeclaration awaiterFieldDef = new FieldDefDeclaration
                                                              {
                                                                  Name = "<>t__dispatchAwaiter",
                                                                  FieldType = this.parent.yieldAwaiter_Type,
                                                                  Attributes = FieldAttributes.Private
                                                              };
                    targetType.Fields.Add( awaiterFieldDef );
                    IField awaiterField = awaiterFieldDef.GetCanonicalGenericInstance();

                    // Find other fields.
                    IField thisField = targetType.Fields.Single<FieldDefDeclaration>( f => f.Name.EndsWith( "__this" ) ).GetCanonicalGenericInstance();
                    IField builderField = targetType.Fields.GetByName( "<>t__builder" ).GetCanonicalGenericInstance();

                    // Emit instructions.
                    InstructionBlock myBlock = context.InstructionBlock.AddChildBlock( null, NodePosition.After, null );
                    InstructionBlock theirBlock = context.InstructionBlock.AddChildBlock( null, NodePosition.After, null );

                    LocalVariableSymbol awaitableLocal = myBlock.DefineLocalVariable(this.parent.task_Yield_Method.ReturnType, "awaitable");
                    LocalVariableSymbol synchronizationContextLocal = myBlock.DefineLocalVariable( this.parent.synchronizationContext_Type, "oldContext" );

                    InstructionSequence entrySequence = myBlock.AddInstructionSequence( null, NodePosition.After, null );
                    InstructionSequence exitSequence = myBlock.AddInstructionSequence( null, NodePosition.After, null );
                    InstructionWriter writer = new InstructionWriter();
                    writer.AttachInstructionSequence( entrySequence );

                    // Emit: if ( this.<>4__this.Dispatcher.CheckAccess() ) goto exitSequence;
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstructionField( OpCodeNumber.Ldfld, thisField );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, this.parent.actor_GetDispatcher_Method );
                    writer.EmitInstructionMethod( OpCodeNumber.Callvirt, this.parent.dispatcher_CheckAccess_Method );
                    writer.EmitBranchingInstruction( OpCodeNumber.Brtrue, exitSequence );

                    // Emit: this.<>t__dispatchAwaiter = Task.Yield().GetAwaiter()
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, this.parent.task_Yield_Method );
                    writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, awaitableLocal );
                    writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca, awaitableLocal);
                    writer.EmitInstructionMethod( OpCodeNumber.Call, this.parent.yieldAwaitable_GetAwaiter_Method );
                    writer.EmitInstructionField( OpCodeNumber.Stfld, awaiterField );

                    // Emit: oldContext = SynchronizationContext.Current
                    writer.EmitInstructionMethod( OpCodeNumber.Call, this.parent.synchronizationContext_getCurrent_Method );
                    writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, synchronizationContextLocal );

                    // Emit: SynchronizationContext.SetSynchronizationContext(this.<>4__this.Dispatcher.SynchronizationContext)
                    writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    writer.EmitInstructionField(OpCodeNumber.Ldfld, thisField);
                    writer.EmitInstructionMethod(OpCodeNumber.Call, this.parent.actor_GetDispatcher_Method);
                    writer.EmitInstructionMethod( OpCodeNumber.Callvirt, this.parent.dispatcher_getSynchronizationContext_Method );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, this.parent.synchronizationContext_SetSynchronizationContext_Method );

                    // Choose which AwaitUnsafeOnCompleted method to call.
                    IGenericMethodDefinition awaitUnsafeOnCompletedMethod;
                    ITypeSignature[] awaitUnsafeOnCompletedGenericTypeParameters;
                    if ( builderField.FieldType == this.parent.asyncVoidMethodBuilder_Type )
                    {
                        awaitUnsafeOnCompletedMethod = this.parent.asyncVoidMethodBuilder_AwaitUnsafeOnCompleted_Method;
                        awaitUnsafeOnCompletedGenericTypeParameters = null;
                    }
                    else if ( builderField.FieldType == this.parent.asyncTaskMethodBuilder_Type )
                    {
                        awaitUnsafeOnCompletedMethod = this.parent.asyncTaskMethodBuilder_AwaitUnsafeOnCompleted_Method;
                        awaitUnsafeOnCompletedGenericTypeParameters = null;
                    }
                    else
                    {
                        // This is a generic task.
                        awaitUnsafeOnCompletedMethod = this.parent.asyncTaskMethodBuilderGeneric_AwaitUnsafeOnCompleted_Method;
                        awaitUnsafeOnCompletedGenericTypeParameters =
                            builderField.FieldType.GetGenericContext( GenericContextOptions.None ).GetGenericTypeParameters();
                    }

                    IMethod awaitUnsafeOnCompletedGenericMethod =
                        awaitUnsafeOnCompletedMethod.GetGenericInstance( new GenericMap( awaitUnsafeOnCompletedGenericTypeParameters,
                                                                                         new ITypeSignature[]
                                                                                             {
                                                                                                 this.parent.yieldAwaiter_Type,
                                                                                                 targetType.GetCanonicalGenericInstance()
                                                                                             } ) );

                    // Emit: this.<>t__builder.AwaitUnsafeOnCompleted<YieldAwaitable.YieldAwaiter, Player.<Ping>d__2>(ref  this.<>t__dispatchAwaiter, ref this);
                     writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstructionField( OpCodeNumber.Ldflda, builderField );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstructionField( OpCodeNumber.Ldflda, awaiterField );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, awaitUnsafeOnCompletedGenericMethod );
                 
                    // Emit: SynchronizationContext.SetSynchronizationContext( oldContext );
                    writer.EmitInstructionLocalVariable( OpCodeNumber.Ldloc, synchronizationContextLocal );
                    writer.EmitInstructionMethod(OpCodeNumber.Call, this.parent.synchronizationContext_SetSynchronizationContext_Method);


                    writer.EmitBranchingInstruction(OpCodeNumber.Leave, context.LeaveBranchTarget);

                    writer.DetachInstructionSequence();

                    // We are done. Give the pipeline to the next node.
                    context.AddRedirection( theirBlock, context.LeaveBranchTarget );
                }

                public override MethodBodyTransformationOptions GetOptions( MetadataDeclaration originalTargetElement, MethodSemantics semantic )
                {
                    return MethodBodyTransformationOptions.None;
                }
            }

            public override string GetDisplayName( MethodSemantics semantic )
            {
                return "Add actor support to state machine.";
            }
        }
    }
}