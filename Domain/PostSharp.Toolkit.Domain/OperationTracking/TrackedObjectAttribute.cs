#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Toolkit.Domain.Tools;

using System.Linq;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    // TODO analyze inheritance behavior 
    /// <summary>
    /// Early development version!!!
    /// </summary>
    [Serializable]
    [IntroduceInterface(typeof(ITrackedObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    public class TrackedObjectAttribute : InstanceLevelAspect, ITrackedObject
    {
        private ObjectAccessorsMap mapForSerialization;

        private Dictionary<string, MethodSnapshotStrategy> methodAttributes;

        private HashSet<string> trackedFields;

        [NonSerialized]
        private IObjectTracker tracker;

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            //Grab the dependencies map to serialize if, if no other aspect has done it before
            mapForSerialization = ObjectAccessorsMap.GetForSerialization();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            //If dependencies map was serialized within this aspect, copy the data to global map
            if (this.mapForSerialization != null)
            {
                this.mapForSerialization.Restore();
                this.mapForSerialization = null;
            }
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            if (!ObjectAccessorsMap.Map.ContainsKey(type))
            {
                ObjectAccessorsMap.Map.Add(type, new ObjectAccessors(type));
            }

            methodAttributes = new Dictionary<string, MethodSnapshotStrategy>();

            foreach (MethodInfo method in type.GetMethods(BindingFlagsSet.PublicInstanceDeclared))
            {
                if (method.GetCustomAttributes(typeof(DoNotMakeAutomaticOperationAttribute), true).Any())
                {
                    this.methodAttributes.Add(method.Name, MethodSnapshotStrategy.Never);
                }
                else if (method.GetCustomAttributes(typeof(AlwaysMakeAutomaticOperationAttribute), true).Any())
                {
                    this.methodAttributes.Add(method.Name, MethodSnapshotStrategy.Always);
                }
                else
                {
                    this.methodAttributes.Add(method.Name, MethodSnapshotStrategy.Auto);
                }
            }

            trackedFields = new HashSet<string>();

            foreach (var propertyInfo in type.GetProperties(BindingFlagsSet.AllInstanceDeclared).Where(f => f.IsDefined(typeof(TrackedPropertyAttribute), false)))
            {
                var propertyInfoClosure = propertyInfo;

                var fields = ReflectionSearch.GetDeclarationsUsedByMethod(propertyInfo.GetGetMethod(true))
                    .Select( r => r.UsedDeclaration as FieldInfo )
                    .Where( f => f != null )
                    .Where(f => propertyInfoClosure.PropertyType.IsAssignableFrom(f.FieldType))
                    .Where(f => f.DeclaringType.IsAssignableFrom( type ) )
                    .ToList();

                if (fields.Count() != 1)
                {
                    DomainMessageSource.Instance.Write( propertyInfo, SeverityType.Error, "INPC013", propertyInfo.Name );
                }
                else
                {
                    trackedFields.Add(fields.First().Name);
                }
            }

            foreach (FieldInfo fieldInfo in type
                                            .GetFields(BindingFlagsSet.AllInstanceDeclared)
                                            .Where(f => f.IsDefined(typeof(TrackedPropertyAttribute), false)))
            {
                trackedFields.Add(fieldInfo.Name);
            }

            base.CompileTimeInitialize(type, aspectInfo);
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

        public override object CreateInstance(AdviceArgs adviceArgs)
        {
            TrackedObjectAttribute aspect = (TrackedObjectAttribute)base.CreateInstance(adviceArgs);

            aspect.SetTracker(new SingleObjectTracker((ITrackable)adviceArgs.Instance));

            return aspect;
        }

        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectMethods")]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            var methodStrategy = methodAttributes[args.Method.Name];
            bool chunkStarted = false;
            ITrackedObject stackPeek = StackTrace.StackPeek() as ITrackedObject;
            if (methodStrategy == MethodSnapshotStrategy.Always ||
               (methodStrategy == MethodSnapshotStrategy.Auto && (stackPeek == null || !ReferenceEquals(stackPeek.Tracker, ThisTracker))))
            {
                ThisTracker.StartChunk();
                chunkStarted = true;
            }
            try
            {
                StackTrace.PushOnStack(args.Instance);
                args.Proceed();
            }
            finally
            {
                StackTrace.PopFromStack();
                if (chunkStarted)
                {
                    ThisTracker.EndChunk();
                }
            }
        }

        private IEnumerable<MethodBase> SelectMethods(Type type)
        {
            return
                type.GetMethods(BindingFlagsSet.PublicInstanceDeclared).Where(
                    m =>
                    m.IsDefined(typeof(AlwaysMakeAutomaticOperationAttribute), true) ||
                    (!m.Name.StartsWith("get_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_")));
            // .Where( m => !m.IsDefined( typeof(DoNotMakeAutomaticSnapshotAttribute), true ) );
        }

        [OnLocationSetValueAdvice]
        [MethodPointcut("SelectFields")]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            bool endChunk = false;
            if (!ThisTracker.IsChunkActive)
            {
                endChunk = true;
                ThisTracker.StartChunk();
            }

            object oldValue = args.GetCurrentValue();

            if (oldValue != null && trackedFields.Contains(args.LocationFullName))
            {
                ITrackedObject trackedObject = (ITrackedObject)oldValue;
                IObjectTracker newTracker = new SingleObjectTracker(trackedObject);
                newTracker.ParentTracker = this.ThisTracker.ParentTracker;
                trackedObject.SetTracker(newTracker);
            }

            args.ProceedSetValue();
            object newValue = args.Value;

            if (newValue != null && trackedFields.Contains(args.LocationName))
            {
                ITrackedObject trackedObject = (ITrackedObject)newValue;
                if (trackedObject.Tracker == null || trackedObject.Tracker.OperationCount != 0)
                {
                    throw new ArgumentException("attaching modified object to aggregate is not supported");
                }

                trackedObject.SetTracker(ThisTracker);
            }

            ThisTracker.AddOperationToChunk(new FieldOperation((ITrackable)this.Instance, args.Location.DeclaringType, args.LocationFullName, oldValue, newValue));

            if (endChunk)
            {
                ThisTracker.EndChunk();
            }

        }

        private IEnumerable<FieldInfo> SelectFields(Type type)
        {
            // Select only fields that are relevant
            return type.GetFields(BindingFlagsSet.AllInstanceDeclared);
        }

        public void Undo()
        {
            this.ThisTracker.Undo();
        }

        public void Redo()
        {
            this.ThisTracker.Redo();
        }

        public void AddNamedRestorePoint(string name)
        {
            this.ThisTracker.AddNamedRestorePoint(name);
        }

        public void RestoreNamedRestorePoint(string name)
        {
            this.ThisTracker.RestoreNamedRestorePoint(name);
        }

        public IObjectTracker Tracker
        {
            get
            {
                return this.tracker;
            }
        }

        public void SetTracker(IObjectTracker tracker)
        {
            this.tracker = tracker;
        }

        public int OperationCount { get; private set; }

        public IObjectTracker ThisTracker
        {
            get
            {
                return ((ITrackedObject)this.Instance).Tracker;
            }
        }

        private enum MethodSnapshotStrategy
        {
            Always,
            Never,
            Auto,
        }
    }
}