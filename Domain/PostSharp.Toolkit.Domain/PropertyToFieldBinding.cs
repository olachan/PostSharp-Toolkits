using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain
{
    // Two way field to property, one to many mapping. 
    [Serializable]
    internal sealed class PropertyToFieldBiDirectionalBinding
    {
        private static readonly List<FieldValueBinding> empty = new List<FieldValueBinding>(0);

        private readonly Dictionary<string, FieldValueBinding> propertyToFieldMapping;
        private readonly Dictionary<string, List<string>> fieldToPropertyMapping;

        public PropertyToFieldBiDirectionalBinding()
        {
            this.propertyToFieldMapping = new Dictionary<string, FieldValueBinding>();
            this.fieldToPropertyMapping = new Dictionary<string, List<string>>();
        }

        private PropertyToFieldBiDirectionalBinding(PropertyToFieldBiDirectionalBinding prototype)
        {
            this.propertyToFieldMapping = prototype.propertyToFieldMapping.ToDictionary(kv => kv.Key, kv => new FieldValueBinding(kv.Value));
            this.fieldToPropertyMapping = prototype.fieldToPropertyMapping.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void AddBinding(string property, FieldInfo field, Type type)
        {
            FieldValueBinding fieldValueBinding = new FieldValueBinding( property, field, type );
            this.propertyToFieldMapping.Add( property, fieldValueBinding );

            List<string> propertyList;
            if ( !this.fieldToPropertyMapping.TryGetValue( fieldValueBinding.Field.FieldName, out propertyList ) )
            {
                propertyList = new List<string>();
                this.fieldToPropertyMapping.Add(fieldValueBinding.Field.FieldName, propertyList);
            }

            propertyList.Add( property );
        }

        public List<FieldValueBinding> GetDependentPropertiesBindings(string fieldName)
        {
            List<string> list;
            if (!this.fieldToPropertyMapping.TryGetValue(fieldName, out list))
            {
                return empty;
            }

            return list.Select( p => this.propertyToFieldMapping[p] ).ToList();
        }

        public bool TryGetSourceFieldBinding(string property, out FieldValueBinding second)
        {
            return this.propertyToFieldMapping.TryGetValue(property, out second);
        }

        public void RuntimeInitialize()
        {
            foreach (FieldValueBinding dependency in this.propertyToFieldMapping.Values)
            {
                dependency.Field.RuntimeInitialize();
            }
        }

        public static PropertyToFieldBiDirectionalBinding CreateFromPrototype(PropertyToFieldBiDirectionalBinding prototype)
        {
            return new PropertyToFieldBiDirectionalBinding(prototype);
        }
    }
}
