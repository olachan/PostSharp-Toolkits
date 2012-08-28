#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Extensibility;
using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    // TODO analyze inheritance behavior 
    /// <summary>
    /// Early development version!!!
    /// </summary>
    [Serializable]
    [IntroduceInterface(typeof(ITrackedObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    public class TrackedObjectAttribute : TrackedObjectAttributeBase
    {
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
            bool endChunk = false;
            if (!this.ThisTracker.IsChunkActive)
            {
                endChunk = true;
                this.ThisTracker.StartChunk();
            }

            object oldValue = args.GetCurrentValue();

            if (oldValue != null && this.TrackedFields.Contains(args.LocationFullName))
            {
                ITrackedObject trackedObject = (ITrackedObject)oldValue;
                IObjectTracker newTracker = new ObjectTracker(trackedObject);
                newTracker.ParentTracker = this.ThisTracker.ParentTracker;
                trackedObject.SetTracker(newTracker);
            }

            args.ProceedSetValue();
            object newValue = args.Value;

            if (newValue != null && this.TrackedFields.Contains(args.LocationName))
            {
                ITrackedObject trackedObject = (ITrackedObject)newValue;
                if (trackedObject.Tracker == null || trackedObject.Tracker.OperationCount != 0)
                {
                    throw new ArgumentException("attaching modified object to aggregate is not supported");
                }

                trackedObject.SetTracker(this.ThisTracker);
            }

            this.ThisTracker.AddToCurrentOperation(new FieldValueChange((ITrackable)this.Instance, args.Location.DeclaringType, args.LocationFullName, oldValue, newValue));

            if (endChunk)
            {
                this.ThisTracker.EndChunk();
            }

        }

        protected IEnumerable<FieldInfo> SelectFields(Type type)
        {
            // Select only fields that are relevant
            return type.GetFields(BindingFlagsSet.AllInstanceDeclared);
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