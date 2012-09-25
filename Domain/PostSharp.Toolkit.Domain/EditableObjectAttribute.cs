using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Toolkit.Domain.ChangeTracking;
using PostSharp.Toolkit.Domain.Common;

namespace PostSharp.Toolkit.Domain
{
    [Serializable]
    [IntroduceInterface(typeof(IEditableObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    public class EditableObjectAttribute : ChangeTrackingAspectBase, IEditableObject
    {
        private Dictionary<MethodBase, MethodBase> interfaceImplementationMap;

        [NonSerialized]
        private AggregateTracker privateTracker;

        [NonSerialized]
        private RestorePointToken restorePointToken;

        private Stack<IDisposable> implicitOperationStack;

        public override object CreateInstance(AdviceArgs adviceArgs)
        {
            EditableObjectAttribute aspect = (EditableObjectAttribute)base.CreateInstance(adviceArgs);
            aspect.privateTracker = new AggregateTracker( adviceArgs.Instance, true );
            aspect.implicitOperationStack = new Stack<IDisposable>();
            return aspect;
        }

        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectMethods")]
        [ProvideAspectRole("OT_ChunkManagement")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "OT_EditableObjectImplementation")]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            this.implicitOperationStack.Push(this.privateTracker.StartImplicitOperationScope(string.Empty));
            try
            {
                args.Proceed();
            }
            finally
            {
                this.implicitOperationStack.Pop().Dispose();
            }
        }

        protected IEnumerable<MethodBase> SelectMethods(Type type)
        {
            return type.GetMethods(BindingFlagsSet.PublicInstanceDeclared).Where(m => !m.IsEventAccessor());
        }

        [OnLocationSetValueAdvice]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_FieldTracking")]
        [MethodPointcut("SelectFields")]
        public void PrivateTrackerOnFieldSet(LocationInterceptionArgs args)
        {
            using (this.privateTracker.StartImplicitOperationScope(string.Empty))
            {
                object oldValue = args.GetCurrentValue();
                args.ProceedSetValue();
                object newValue = args.Value;
                this.privateTracker.AddToCurrentOperation(new FieldValueChange(this.Instance, args.Location.DeclaringType, args.LocationFullName, oldValue, newValue));
            }
        }

        protected IEnumerable<FieldInfo> SelectFields(Type type)
        {
            var ignoredFields = this.GetFieldsWithAttribute(type, typeof(ChangeTrackingIgnoreField), "DOM015");
            return type.GetFields(BindingFlagsSet.AllInstanceDeclared).Where(f => !ignoredFields.Contains(f.FullName()));
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            base.CompileTimeInitialize(type, aspectInfo);


            if (!typeof(IEditableObject).IsAssignableFrom(type))
            {
                this.interfaceImplementationMap = new Dictionary<MethodBase, MethodBase>();
                return;
            }

            if (typeof(IEditableObject).IsAssignableFrom(type))
            {
                var targetIEditableMethodsInterfaceMap = type.GetInterfaceMap( typeof(IEditableObject) );
                var aspectIEditableMethodsInterfaceMap = this.GetType().GetInterfaceMap( typeof(IEditableObject) );

                var targetIEditableMethodsMap = targetIEditableMethodsInterfaceMap.InterfaceMethods.Zip(
                    targetIEditableMethodsInterfaceMap.TargetMethods, ( im, tm ) => new { InterfaceMethod = im, TargetMethod = tm } )
                    .ToList();

                if (!targetIEditableMethodsMap.All( t => t.TargetMethod.IsToBeImplementedMethod() ))
                {
                    DomainMessageSource.Instance.Write(type, SeverityType.Error, "DOM016", type.FullName);
                }

                var aspectIEditableMethodsMap = aspectIEditableMethodsInterfaceMap.InterfaceMethods.Zip(
                    aspectIEditableMethodsInterfaceMap.TargetMethods, (im, tm) => new { InterfaceMethod = im, TargetMethod = tm })
                    .ToList();
                
                this.interfaceImplementationMap = 
                    targetIEditableMethodsMap
                    .Join(
                        aspectIEditableMethodsMap,
                        m => m.InterfaceMethod,
                        m => m.InterfaceMethod,
                        ( tm, am ) => new { TargetMethod = (MethodBase)tm.TargetMethod, AspectMethod = (MethodBase)am.TargetMethod } )
                    .ToDictionary(r => r.TargetMethod, r => r.AspectMethod );
            }
        }

        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectEditableObjectImplementingMethods")]
        [ProvideAspectRole("OT_EditableObjectImplementation")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, "OT_ChunkManagement")]
        public void OnEditableObjectImplementationInvoke(MethodInterceptionArgs args)
        {
            if (!this.interfaceImplementationMap.ContainsKey(args.Method))
            {
                args.Proceed();
            }

            args.ReturnValue = this.interfaceImplementationMap[args.Method].Invoke(this, new object[0]);
        }

        private IEnumerable<MethodBase> SelectEditableObjectImplementingMethods(Type type)
        {
            return type.GetInterfaceMap(typeof(IEditableObject)).TargetMethods;
        }


        public void BeginEdit()
        {
            if (this.restorePointToken != null)
            {
                throw new InvalidOperationException("BeginEdit is not reentrant.");
            }

            this.restorePointToken = this.privateTracker.AddRestorePoint();
        }

        public void EndEdit()
        {
            if (this.restorePointToken == null)
            {
                throw new InvalidOperationException("BeginEdit must be called prior to EndEdit.");
            }

            this.restorePointToken = null;
        }

        public void CancelEdit()
        {
            if (this.restorePointToken == null)
            {
                throw new InvalidOperationException("BeginEdit must be called prior to EndEdit.");
            }

            this.privateTracker.UndoTo( this.restorePointToken );
            this.restorePointToken = null;

        }

        public override void RuntimeInitialize(Type type)
        {
            var fieldAccessors = ObjectAccessorsMap.Map[type].FieldAccessors.Values;
            foreach (FieldInfoWithCompiledAccessors fieldAccessor in fieldAccessors)
            {
                //TODO performance impact?
                fieldAccessor.RuntimeInitialize();
            }

            base.RuntimeInitialize(type);
        }
    }
}