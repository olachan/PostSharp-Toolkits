#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Toolkit.Domain.Tools;

using System.Linq;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    // TODO analyze inheritance behavior 
    [Serializable]
    [IntroduceInterface(typeof(ITrackedObject))]
    public class TrackedObjectAttribute : InstanceLevelAspect, ITrackedObject
    {
        private ObjectAccessorsMap mapForSerialization;

        [NonSerialized]
        private SingleObjectTracker tracker;

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
            if (!ObjectAccessorsMap.Map.ContainsKey( type ))
            {
                ObjectAccessorsMap.Map.Add( type, new ObjectAccessors( type ) );
            }

            base.CompileTimeInitialize(type, aspectInfo);
        }

        public override void RuntimeInitialize(Type type)
        {
            var fieldAccessors = ObjectAccessorsMap.Map[type].FieldAccessors.Values;
            foreach ( FieldInfoWithCompiledAccessors fieldAccessor in fieldAccessors )
            {
                //TODO performance impact?
                fieldAccessor.RuntimeInitialize();
            }

            base.RuntimeInitialize(type);
        }

        public override object CreateInstance(AdviceArgs adviceArgs)
        {
            TrackedObjectAttribute aspect = (TrackedObjectAttribute)base.CreateInstance(adviceArgs);

            aspect.tracker = new SingleObjectTracker((ITrackable)adviceArgs.Instance);

            return aspect;
        }

        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectMethods")]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            if (StackTrace.StackPeek() != args.Instance)
            {
                tracker.AddObjectSnapshot();
            }
            try
            {
                StackTrace.PushOnStack(args.Instance);
                args.Proceed();
            }
            finally
            {
                StackTrace.PopFromStack();
                //if (StackTrace.StackPeek() != args.Instance)
                //{
                //    tracker.AddObjectSnapshot();
                //}
            }
        }

        private IEnumerable<MethodBase> SelectMethods(Type type)
        {
            return type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_"))
                .Where( m => !m.IsDefined( typeof(DoNotTrackAttribute), true ) );
        }

        public ISnapshot TakeSnapshot()
        {
            return new FieldSnapshot( (ITrackable)this.Instance );
        }

        public void Undo()
        {
            this.tracker.Undo();
        }

        public void Redo()
        {
            this.tracker.Redo();
        }

        public void AddObjectSnapshot( string name )
        {
            this.tracker.AddObjectSnapshot( name );
        }

        public void AddObjectSnapshot()
        {
            this.tracker.AddObjectSnapshot();
        }

        public void AddNamedRestorePoint( string name )
        {
            this.tracker.AddNamedRestorePoint( name );
        }

        public void RestoreNamedRestorePoint( string name )
        {
            this.tracker.RestoreNamedRestorePoint( name );
        }
    }
}