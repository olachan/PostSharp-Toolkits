using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain
{
    [Serializable]
    internal sealed class PropertyToFieldBiDirectionalMap
    {
        Dictionary<string, FieldByValueDependency> propertyToField;
        Dictionary<string, List<string>> fieldToProperty;

        private static readonly List<KeyValuePair<string,FieldByValueDependency>> empty = new List<KeyValuePair<string, FieldByValueDependency>>(0);

        public PropertyToFieldBiDirectionalMap()
        {
            propertyToField = new Dictionary<string, FieldByValueDependency>();
            fieldToProperty = new Dictionary<string, List<string>>();
        }

        public PropertyToFieldBiDirectionalMap(PropertyToFieldBiDirectionalMap prototype)
        {
            propertyToField = prototype.propertyToField.ToDictionary(kv => kv.Key, kv => new FieldByValueDependency(kv.Value));
            fieldToProperty = prototype.fieldToProperty.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void Add(string property, FieldByValueDependency field)
        {
            propertyToField.Add( property, field );

            List<string> propertyList;
            if ( !fieldToProperty.TryGetValue( field.Field.FieldName, out propertyList ) )
            {
                propertyList = new List<string>();
                fieldToProperty.Add(field.Field.FieldName, propertyList);
            }

            propertyList.Add( property );
        }

        public List<KeyValuePair<string,FieldByValueDependency>> GetByField(string fieldName)
        {
            List<string> list;
            if (!fieldToProperty.TryGetValue(fieldName, out list))
            {
                return empty;
            }

            List<KeyValuePair<string, FieldByValueDependency>> result = new List<KeyValuePair<string, FieldByValueDependency>>();

            foreach ( var propertyName in list )
            {
                result.Add( new KeyValuePair<string, FieldByValueDependency>( propertyName, propertyToField[propertyName] ));
            }

            return result;
        }

        public bool TryGetByProperty(string property, out FieldByValueDependency second)
        {
            return this.propertyToField.TryGetValue(property, out second);
        }

        public void RuntimeInitialize()
        {
            foreach (FieldByValueDependency dependency in propertyToField.Values)
            {
                dependency.Field.RuntimeInitialize();
            }
        }
    }

    [Serializable]
    internal sealed class FieldByValueDependency : IEquatable<FieldByValueDependency>
    {
        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Field != null ? this.Field.FieldName.GetHashCode() : 0) * 397) ^ this.IsActive.GetHashCode();
            }
        }

        public FieldByValueDependency(FieldInfo field, Type type)
        {
            this.Field = new FieldInfoWithGetter(field, type);
            this.IsActive = true;
        }

        public FieldByValueDependency(FieldByValueDependency prototype)
        {
            this.Field = prototype.Field;
            this.IsActive = true;
        }

        public FieldInfoWithGetter Field { get; private set; }

        public bool IsActive { get; set; }

        public override bool Equals(object obj)
        {
            FieldByValueDependency other = obj as FieldByValueDependency;
            return this.Equals(other);
        }

        public bool Equals(FieldByValueDependency other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Field.FieldName == other.Field.FieldName && this.IsActive == other.IsActive;
        }
    }

    [Serializable]
    internal sealed class FieldInfoWithGetter
    {
        public FieldInfoWithGetter(FieldInfo field, Type type)
        {
            location = new LocationInfo(field);
            this.type = type;
        }

        public void RuntimeInitialize()
        {
            if (GetValue == null)
            {
                ParameterExpression objectParameterExpression = Expression.Parameter( typeof(object) );
                UnaryExpression castExpression = Expression.Convert( objectParameterExpression, type );
                Expression fieldExpr = Expression.PropertyOrField( castExpression, location.Name );
                UnaryExpression resultCastExpression = Expression.Convert( fieldExpr, typeof(object) );
                GetValue = Expression.Lambda<Func<object, object>>( resultCastExpression, objectParameterExpression ).Compile();
            }
        }

        private LocationInfo location;

        private Type type;

        public string FieldName
        {
            get
            {
                return location.Name;
            }
        }

        public Func<object, object> GetValue { get; private set; }
    }
}
