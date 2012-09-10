using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Toolkit.Domain.Common;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [Serializable]
    public abstract class ChangeTrackingAspectBase : InstanceLevelAspect
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

        protected HashSet<string> GetFieldsWithAttribute(Type type, Type attributeType, string error)
        {
            HashSet<string> fieldSet = new HashSet<string>();

            foreach (var propertyInfo in type.GetProperties(BindingFlagsSet.AllInstanceDeclared).Where(f => f.IsDefined(attributeType, true)))
            {
                var propertyInfoClosure = propertyInfo;

                var fields =
                    ReflectionSearch.GetDeclarationsUsedByMethod(propertyInfo.GetGetMethod(true))
                        .Select(r => r.UsedDeclaration as FieldInfo)
                        .Where(f => f != null)
                        .Where(f => propertyInfoClosure.PropertyType.IsAssignableFrom(f.FieldType))
                        .Where(f => f.DeclaringType.IsAssignableFrom(type))
                        .ToList();

                if (fields.Count() != 1)
                {
                    DomainMessageSource.Instance.Write(propertyInfo, SeverityType.Error, error, propertyInfo.FullName());
                }
                else
                {
                    fieldSet.Add(fields.First().FullName());
                }
            }

            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlagsSet.AllInstanceDeclared).Where(f => f.IsDefined(attributeType, true)))
            {
                fieldSet.Add(fieldInfo.FullName());
            }

            return fieldSet;
        }
    }
}