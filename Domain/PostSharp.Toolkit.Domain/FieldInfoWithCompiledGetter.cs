using System;
using System.Linq.Expressions;
using System.Reflection;

using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain
{
    [Serializable]
    internal sealed class FieldInfoWithCompiledGetter
    {
        private readonly LocationInfo location;
        private readonly Type type;

        public FieldInfoWithCompiledGetter(FieldInfo field, Type type)
        {
            this.location = new LocationInfo(field);
            this.type = type;
        }

        public string FieldName
        {
            get
            {
                return this.location.Name;
            }
        }

        public Func<object, object> GetValue { get; set; }

        public void RuntimeInitialize()
        {
            if (this.GetValue == null)
            {
                ParameterExpression objectParameterExpression = Expression.Parameter(typeof(object));
                UnaryExpression castExpression = Expression.Convert(objectParameterExpression, this.type);
                string locationName = (this.location.FieldInfo == null) ? this.location.PropertyInfo.Name : this.location.FieldInfo.Name;
                Expression fieldExpr = PropertyOrFieldCaseSensitive(castExpression, locationName);
                UnaryExpression resultCastExpression = Expression.Convert(fieldExpr, typeof(object));
                this.GetValue = Expression.Lambda<Func<object, object>>(resultCastExpression, objectParameterExpression).Compile();
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