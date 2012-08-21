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
using PostSharp.Toolkit.Domain.Tools;

using System.Linq;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    // TODO analyze inheritance behavior 
    /// <summary>
    /// Early development version!!!
    /// </summary>
    [Serializable]
    [IntroduceInterface(typeof(ITrackedObject))]
    public class TrackedObjectAttribute : InstanceLevelAspect, ITrackedObject
    {
        private ObjectAccessorsMap mapForSerialization;

        private Dictionary<string, MethodOperationStrategy> methodAttributes;

        private bool hasTrackedProperties;
        
        [NonSerialized]
        private ObjectTracker tracker;

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            //TODO: Consider better serialization mechanism

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

            methodAttributes = new Dictionary<string, MethodOperationStrategy>();

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttributes( typeof(DoNotMakeAutomaticOperationAttribute), true ).Any())
                {
                    this.methodAttributes.Add( method.Name, MethodOperationStrategy.Never);
                }
                else if (method.GetCustomAttributes(typeof(AlwaysMakeAutomaticOperationAttribute), true).Any())
                {
                    this.methodAttributes.Add(method.Name, MethodOperationStrategy.Always);
                }
                else
                {
                    this.methodAttributes.Add(method.Name, MethodOperationStrategy.Auto);
                }
            }

            hasTrackedProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Any(m => m.IsDefined(typeof(TrackedPropertyAttribute), true));

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

            aspect.tracker = new SingleObjectTracker((ITrackable)adviceArgs.Instance);// hasTrackedProperties ? (ObjectTracker)new AggregateTracker((ITrackable)adviceArgs.Instance) : new SingleObjectTracker((ITrackable)adviceArgs.Instance);

            return aspect;
        }

        // TODO cope with field initializers
        //[OnLocationSetValueAdvice]
        //[MethodPointcut("SelectTrackedProperties")]
        //public void OnTrackedPropertySet(LocationInterceptionArgs args)
        //{
        //    ITrackedObject trackedObject = (ITrackedObject)args.Value;

        //    if (trackedObject != null)
        //    {
        //        ((AggregateTracker)this.tracker).RemoveDependentTracker( trackedObject.Tracker );
        //    }

        //    args.ProceedSetValue();

        //    trackedObject = (ITrackedObject)args.Value;

        //    if (trackedObject != null)
        //    {
        //        ((AggregateTracker)this.tracker).AddDependentTracker(trackedObject.Tracker);
        //    }
        //}

        //private IEnumerable<PropertyInfo> SelectTrackedProperties(Type type)
        //{
        //    return
        //        type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        //        .Where(m => m.IsDefined(typeof(TrackedPropertyAttribute), true));
        //}

        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectMethods")]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            // TODO: method attribute compile time map

            var methodStrategy = methodAttributes[args.Method.Name];
            bool chunkStarted = false;

            if ((StackTrace.StackPeek() != args.Instance && methodStrategy == MethodOperationStrategy.Auto) || 
                methodStrategy == MethodOperationStrategy.Always)
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
                StackTrace.PopFromStack(); //TODO: snapshot add strategy
                if (chunkStarted)
                {
                    ThisTracker.EndChunk();
                }
                //if (StackTrace.StackPeek() != args.Instance)
                //{
                //    tracker.AddObjectSnapshot();
                //}
            }
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
            args.ProceedSetValue();
            object newValue = args.Value;

            ThisTracker.AddOperationToChunk( new FieldOperation( (ITrackable)this.Instance, args.LocationFullName, oldValue, newValue ) );

            if (endChunk)
            {
                ThisTracker.EndChunk();
            }

        }

        private IEnumerable<FieldInfo> SelectFields(Type type)
        {
            // Select only fields that are relevant
            return type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly );
        }

        private IEnumerable<MethodBase> SelectMethods(Type type)
        {
            return
                type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(
                    m =>
                    m.IsDefined(typeof(AlwaysMakeAutomaticOperationAttribute), true) ||
                    (!m.Name.StartsWith("get_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_")));
            // .Where( m => !m.IsDefined( typeof(DoNotMakeAutomaticOperationAttribute), true ) );
        }

        //public IOperation TakeSnapshot()
        //{
        //    return new FieldOperation((ITrackable)this.Instance);
        //}

        public void Undo()
        {
            this.ThisTracker.Undo();
        }

        public void Redo()
        {
            this.ThisTracker.Redo();
        }

        //public void AddObjectSnapshot(string name)
        //{
        //    this.tracker.AddObjectSnapshot(name);
        //}

        //public void AddObjectSnapshot()
        //{
        //    this.tracker.AddObjectSnapshot();
        //}

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

        public IObjectTracker ThisTracker
        {
            get
            {
                return ((ITrackedObject)this.Instance).Tracker;
            }
        }

        private enum MethodOperationStrategy
        {
            Always,
            Never,
            Auto,
        }
    }
}