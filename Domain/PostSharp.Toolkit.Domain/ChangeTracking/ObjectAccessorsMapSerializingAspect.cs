using System;
using System.Runtime.Serialization;

using PostSharp.Aspects;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [Serializable]
    public abstract class ObjectAccessorsMapSerializingAspect : InstanceLevelAspect
    {
        private ObjectAccessorsMap mapForSerialization;

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            //Grab the dependencies map to serialize if, if no other aspect has done it before
            this.mapForSerialization = ObjectAccessorsMap.GetForSerialization();
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
            if ( !ObjectAccessorsMap.Map.ContainsKey( type ) )
            {
                ObjectAccessorsMap.Map.Add( type, new ObjectAccessors( type ) );
            }
        }
    }
}