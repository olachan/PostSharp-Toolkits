#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Extensibility;
using PostSharp.Toolkit.Domain.ChangeTracking;
using PostSharp.Toolkit.Domain.Common;

namespace PostSharp.Toolkit.Domain
{
    // TODO analyze inheritance behavior 
    /// <summary>
    /// Early development version!!!
    /// </summary>
    [Serializable]
    [IntroduceInterface(typeof(ITrackedObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    public class TrackedObjectAttribute : ImplicitOperationManagementAttribute
    {
        public TrackedObjectAttribute( bool enableTrackingOnTrackerCreation = false )
            : base( enableTrackingOnTrackerCreation )
        {
        }

        private string FieldSetOperationStringFormat
        {
            get
            {
                return this.ThisTracker.NameGenerationConfiguration.FieldSetOperationStringFormat;
            }
        }

        [OnMethodInvokeAdvice]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_EventHook")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_EventRaise")]
        [MethodPointcut("SelectMethods")]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            this.OnMethodInvokeBase(args);
        }


        [OnLocationSetValueAdvice]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_FieldTracking")]
        [MethodPointcut("SelectFields")]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            using (this.ThisTracker.StartImplicitOperationScope(string.Format(this.FieldSetOperationStringFormat, args.LocationName)))
            {
                object oldValue = args.GetCurrentValue();

                if (oldValue != null && this.TrackedFields.Contains(args.LocationFullName))
                {
                    this.ThisTracker.DetachFromAggregate( oldValue, this.EnableTrackingOnTrackerCreation );
                }

                args.ProceedSetValue();
                object newValue = args.Value;

                if (newValue != null && this.TrackedFields.Contains(args.LocationFullName))
                {
                    this.ThisTracker.AttachToAggregate( newValue );
                }

                this.ThisTracker.AddToCurrentOperation(new FieldValueChange(this.Instance, args.Location.DeclaringType, args.LocationFullName, oldValue, newValue));
            }
        }

        protected IEnumerable<FieldInfo> SelectFields(Type type)
        {
            var ignoredFields = this.GetFieldsWithAttribute(type, typeof(ChangeTrackingIgnoreField), "DOM015");
            return type.GetFields(BindingFlagsSet.AllInstanceDeclared).Where(f => !ignoredFields.Contains(f.FullName()));
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