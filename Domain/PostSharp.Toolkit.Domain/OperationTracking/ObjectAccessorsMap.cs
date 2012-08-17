using System;
using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    [Serializable]
    internal class ObjectAccessorsMap
    {
        private Dictionary<Type, ObjectAccessors> mapForSerialization;

        public static Dictionary<Type, ObjectAccessors> Map { get; private set; }

        static ObjectAccessorsMap()
        {
            Map = new Dictionary<Type, ObjectAccessors>();
        }

        public static ObjectAccessorsMap GetForSerialization()
        {
            if (ObjectAccessorsMap.Map != null)
            {
                Dictionary<Type, ObjectAccessors> map = ObjectAccessorsMap.Map;
                ObjectAccessorsMap.Map = null;
                return new ObjectAccessorsMap(map);
            }

            return null;
        }

        private ObjectAccessorsMap(Dictionary<Type, ObjectAccessors> map)
        {
            this.mapForSerialization = map;
        }

        public void Restore()
        {
            if (this.mapForSerialization == null)
            {
                return;
            }

            this.mapForSerialization.OnDeserialization( this );

            foreach (KeyValuePair<Type, ObjectAccessors> objectAccessor in this.mapForSerialization)
            {
                ObjectAccessorsMap.Map.Add(objectAccessor.Key, objectAccessor.Value);
            }

            this.mapForSerialization = null;
        }
    }
}