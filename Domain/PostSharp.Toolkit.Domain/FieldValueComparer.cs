using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PostSharp.Toolkit.Domain
{
    [Serializable]
    internal class FieldValueComparer 
    {
        private enum FieldType
        {
            ValueType,
            ReferenceType
        }

        private Dictionary<string, FieldType> FieldTypes { get; set; }

        public FieldValueComparer(Type type)
        {
            var fieldTypes = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToDictionary(f => ReflectionHelpers.FullName( f ), f => f.FieldType.IsValueType ? FieldType.ValueType : FieldType.ReferenceType);

            this.FieldTypes = fieldTypes.Union(
                type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToDictionary(
                    f => f.FullName(), f => f.PropertyType.IsValueType ? FieldType.ValueType : FieldType.ReferenceType)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public bool AreEqual(string locationFullName, object currentValue, object newValue)
        {
            FieldType fieldType;
            this.FieldTypes.TryGetValue(locationFullName, out fieldType);

            return fieldType == FieldType.ValueType ? Equals(currentValue, newValue) : ReferenceEquals(currentValue, newValue);
        }
    }
}