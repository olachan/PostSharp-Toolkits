#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PostSharp.Toolkit.Domain.Common;

namespace PostSharp.Toolkit.Domain.PropertyChangeTracking
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

        public FieldValueComparer( Type type )
        {
            Dictionary<string, FieldType> fieldTypes =
                type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).ToDictionary(
                    f => f.FullName(), f => f.FieldType.IsValueType ? FieldType.ValueType : FieldType.ReferenceType );

            this.FieldTypes =
                fieldTypes.Union(
                    type.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).ToDictionary(
                        f => f.FullName(), f => f.PropertyType.IsValueType ? FieldType.ValueType : FieldType.ReferenceType ) ).ToDictionary(
                            kv => kv.Key, kv => kv.Value );
        }

        public bool AreEqual( string locationFullName, object currentValue, object newValue )
        {
            FieldType fieldType;
            this.FieldTypes.TryGetValue( locationFullName, out fieldType );

            return fieldType == FieldType.ValueType ? Equals( currentValue, newValue ) : ReferenceEquals( currentValue, newValue );
        }
    }
}