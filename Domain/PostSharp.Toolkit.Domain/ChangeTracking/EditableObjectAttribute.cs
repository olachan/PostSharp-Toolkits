using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: Need to make sure this IEditable is not part of an aggregate; it it is, we should throw or use a separate tracker
    //TODO: Consider introducing the interfaces separately

    [Serializable]
    [IntroduceInterface(typeof(ITrackedObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [IntroduceInterface(typeof(IEditableObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [IntroduceInterface(typeof(IChangeTracking), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    public class EditableObjectAttribute : TrackedObjectAttribute, IEditableObject, IChangeTracking
    {
        private Guid restorePointName;

        private Dictionary<MethodBase, MethodBase> interfaceImplementationMap;

        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectMethods")]
        [ProvideAspectRole("OT_ChunkManagement")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "OT_EditableObjectImplementation")]
        public new void OnMethodInvoke(MethodInterceptionArgs args)
        {
            base.OnMethodInvoke(args);
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            base.CompileTimeInitialize(type, aspectInfo);

            if (!typeof(IEditableObject).IsAssignableFrom(type) && !typeof(IChangeTracking).IsAssignableFrom(type))
            {
                this.interfaceImplementationMap = new Dictionary<MethodBase, MethodBase>();
                return;
            }

            Dictionary<MethodBase, MethodBase> iEditableMap = null;
            Dictionary<MethodBase, MethodBase> iChangetrackingMap = null;

            if (typeof(IEditableObject).IsAssignableFrom(type))
            {
                var targetIEditableMethodsMap = type.GetInterfaceMap( typeof(IEditableObject) );
                var aspectIEditableMethodsMap = this.GetType().GetInterfaceMap( typeof(IEditableObject) );
               
                iEditableMap = targetIEditableMethodsMap.TargetMethods.Join(
                        aspectIEditableMethodsMap.TargetMethods,
                        m => m.Name,
                        m => m.Name,
                        ( tm, am ) => new { TargetMethod = (MethodBase)tm, AspectMethod = (MethodBase)am } ).ToDictionary(
                            r => r.TargetMethod, r => r.AspectMethod );
            }

            if (typeof(IChangeTracking).IsAssignableFrom(type))
            {
                var targeIChangeTrackingeMethodsMap = type.GetInterfaceMap( typeof(IChangeTracking) );
                var aspectIChangeTrackingMethodsMap = this.GetType().GetInterfaceMap( typeof(IChangeTracking) );
               
                iChangetrackingMap = targeIChangeTrackingeMethodsMap.TargetMethods.Join(
                        aspectIChangeTrackingMethodsMap.TargetMethods,
                        m => m.Name,
                        m => m.Name,
                        ( tm, am ) => new { TargetMethod = (MethodBase)tm, AspectMethod = (MethodBase)am } ).ToDictionary(
                            r => r.TargetMethod, r => r.AspectMethod );
            }

            if (iEditableMap == null)
            {
                this.interfaceImplementationMap = iChangetrackingMap;
            }
            else if(iChangetrackingMap == null)
            {
                this.interfaceImplementationMap = iEditableMap;
            }
            else
            {
                this.interfaceImplementationMap = iEditableMap.Union(iChangetrackingMap).ToDictionary(kv => kv.Key, kv => kv.Value);
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
            return (!typeof(IEditableObject).IsAssignableFrom(type) ?
                    Enumerable.Empty<MethodBase>() :
                    type.GetInterfaceMap(typeof(IEditableObject)).TargetMethods)
                    .Union(!typeof(IChangeTracking).IsAssignableFrom(type) ?
                    Enumerable.Empty<MethodBase>() :
                    type.GetInterfaceMap(typeof(IChangeTracking)).TargetMethods);
        }

        [OnLocationSetValueAdvice]
        [MethodPointcut("SelectFields")]
        public new void OnFieldSet(LocationInterceptionArgs args)
        {
            base.OnFieldSet(args);
        }

        public void BeginEdit()
        {
            if (this.restorePointName != Guid.Empty)
            {
                throw new InvalidOperationException("BeginEdit is not reentrant.");
            }

            this.restorePointName = Guid.NewGuid();
            this.ThisTracker.AddRestorePoint(this.restorePointName.ToString());
            //chunkToken = this.ThisTracker.StartAtomicOperation();
        }

        public void EndEdit()
        {
            if (this.restorePointName == Guid.Empty)
            {
                throw new InvalidOperationException("BeginEdit must be called prior to EndEdit.");
            }

            this.restorePointName = Guid.Empty;
        }

        public void CancelEdit()
        {
            if (this.restorePointName == Guid.Empty)
            {
                throw new InvalidOperationException("BeginEdit must be colled prior to EndEdit.");
            }

            this.UndoToRestorePoint(this.restorePointName.ToString());
            this.restorePointName = Guid.Empty;
        }

        public void AcceptChanges()
        {
            this.ThisTracker.Clear();
        }

        public bool IsChanged
        {
            get
            {
                return this.ThisTracker.OperationsCount != 0;
            }
        }
    }
}