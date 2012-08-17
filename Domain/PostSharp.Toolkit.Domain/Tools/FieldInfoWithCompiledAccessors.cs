using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PostSharp.Toolkit.Domain.Tools
{
    internal delegate void SetFieldValueDelegate(object instance, object value);

    [Serializable]
    internal class FieldInfoWithCompiledAccessors : FieldInfoWithCompiledGetter
    {
        public FieldInfoWithCompiledAccessors( FieldInfo field, Type type )
            : base( field, type )
        {
        }

        public SetFieldValueDelegate SetValue { get; private set; }

        public override void RuntimeInitialize()
        {
            base.RuntimeInitialize();
            
            if (this.SetValue == null)
            {
                ParameterExpression instanceParameterExpression = Expression.Parameter(typeof(object));
                ParameterExpression valueParameterExpression = Expression.Parameter(typeof(object));
                UnaryExpression instanceCastExpression = Expression.Convert(instanceParameterExpression, this.type);
                UnaryExpression valueCastExpression = Expression.Convert(valueParameterExpression, this.location.LocationType);
                string locationName = (this.location.FieldInfo == null) ? this.location.PropertyInfo.Name : this.location.FieldInfo.Name;
                Expression fieldExpr = PropertyOrFieldCaseSensitive(instanceCastExpression, locationName);
                BinaryExpression assignementExpression = Expression.Assign(fieldExpr, valueCastExpression);
                this.SetValue = Expression.Lambda<SetFieldValueDelegate>(assignementExpression, instanceParameterExpression, valueParameterExpression).Compile();
            }
        }
    }
}