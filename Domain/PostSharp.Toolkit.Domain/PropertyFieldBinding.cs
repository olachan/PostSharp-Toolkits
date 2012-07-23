using System;
using System.Linq.Expressions;
using System.Reflection;

using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Represents heuristically identified association of property value with a field.
    /// Denotes that the last time the property getter was called it returned value of the field.
    /// </summary>
    [Serializable]
    internal sealed class PropertyFieldBinding : IEquatable<PropertyFieldBinding>
    {
        public PropertyFieldBinding(string propertyName, FieldInfo field, Type type)
        {
            this.PropertyName = propertyName;
            this.Field = new FieldInfoWithCompiledGetter(field, type);
            this.IsActive = true;
        }

        public PropertyFieldBinding(PropertyFieldBinding prototype)
        {
            this.Field = prototype.Field;
            this.PropertyName = prototype.PropertyName;
            this.IsActive = true;
        }

        public FieldInfoWithCompiledGetter Field { get; private set; }

        public bool IsActive { get; set; }

        public string PropertyName { get; private set; }

        public override bool Equals(object obj)
        {
            PropertyFieldBinding other = obj as PropertyFieldBinding;
            return this.Equals(other);
        }

        public bool Equals(PropertyFieldBinding other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Field.FieldName == other.Field.FieldName && this.IsActive == other.IsActive;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Field != null ? this.Field.FieldName.GetHashCode() : 0) * 397) ^ this.IsActive.GetHashCode();
            }
        }

        // Binding to field with compiled getter for performance
        [Serializable]
        internal sealed class FieldInfoWithCompiledGetter
        {
            private readonly LocationInfo location;
            private readonly Type type;

            public FieldInfoWithCompiledGetter(FieldInfo field, Type type)
            {
                location = new LocationInfo(field);
                this.type = type;
            }

            public string FieldName
            {
                get
                {
                    return location.Name;
                }
            }

            public Func<object, object> GetValue { get; set; }

            public void RuntimeInitialize()
            {
                if (GetValue == null)
                {
                    ParameterExpression objectParameterExpression = Expression.Parameter(typeof(object));
                    UnaryExpression castExpression = Expression.Convert(objectParameterExpression, type);
                    string locationName = (location.FieldInfo == null) ? location.PropertyInfo.Name : location.FieldInfo.Name;
                    Expression fieldExpr = PropertyOrFieldCaseSensitive(castExpression, locationName);
                    UnaryExpression resultCastExpression = Expression.Convert(fieldExpr, typeof(object));
                    GetValue = Expression.Lambda<Func<object, object>>(resultCastExpression, objectParameterExpression).Compile();
                }
            }

            public static MemberExpression PropertyOrFieldCaseSensitive(Expression expression, string propertyOrFieldName)
            {
                PropertyInfo property1 = expression.Type.GetProperty(propertyOrFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (property1 != (PropertyInfo)null)
                    return Expression.Property(expression, property1);
                FieldInfo field1 = expression.Type.GetField(propertyOrFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (field1 != (FieldInfo)null)
                    return Expression.Field(expression, field1);
                else
                    throw new ArgumentException("Invalid field or property name");
            }
        }
    }
}