#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Toolkit.Domain.PropertyDependencyAnalisys;
using PostSharp.Toolkit.Domain.Tools;

using System.Linq;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    // TODO analyze inheritance behavior 
    [Serializable]
    [IntroduceInterface(typeof(IOperationTrackable))]
    public class TrackedObjectAttribute : InstanceLevelAspect, IOperationTrackable
    {
        private ObjectAccessorsMap mapForSerialization;

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

        public Snapshot TakeSnapshot()
        {
            Dictionary<int, FieldInfoWithCompiledAccessors> fieldAccessors = ObjectAccessorsMap.Map[this.Instance.GetType()].FieldAccessors;
            Dictionary<int, object> fieldValues = fieldAccessors.ToDictionary( f => f.Key, f => f.Value.GetValue( this.Instance ) );
            return new FieldSnapshot( (IOperationTrackable)this.Instance, fieldValues );
        }

        public void RestoreSnapshot( Snapshot snapshot )
        {
            FieldSnapshot fieldSnapshot;
            if ((fieldSnapshot = snapshot as FieldSnapshot) == null)
            {
                throw new ArgumentException("Unsupported snapshot type");
            }

            Dictionary<int, FieldInfoWithCompiledAccessors> fieldAccessors = ObjectAccessorsMap.Map[this.Instance.GetType()].FieldAccessors;

            foreach ( KeyValuePair<int, FieldInfoWithCompiledAccessors> fieldAccessor in fieldAccessors )
            {
                fieldAccessor.Value.SetValue( this.Instance, fieldSnapshot.FieldValues[fieldAccessor.Key] );
            }
        }
    }
}