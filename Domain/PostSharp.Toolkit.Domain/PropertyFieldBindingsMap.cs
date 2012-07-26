using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Storage for <see cref="PropertyFieldBinding"/>s
    /// Two way field to property, one to many mapping. 
    /// </summary>
    [Serializable]
    internal sealed class PropertyFieldBindingsMap
    {
        private static readonly List<PropertyFieldBinding> empty = new List<PropertyFieldBinding>(0);

        private readonly Dictionary<string, PropertyFieldBinding> propertyToFieldMapping;
        private readonly Dictionary<string, List<string>> fieldToPropertyMapping;

        public PropertyFieldBindingsMap()
        {
            this.propertyToFieldMapping = new Dictionary<string, PropertyFieldBinding>();
            this.fieldToPropertyMapping = new Dictionary<string, List<string>>();
        }

        private PropertyFieldBindingsMap(PropertyFieldBindingsMap prototype)
        {
            this.propertyToFieldMapping = prototype.propertyToFieldMapping.ToDictionary(kv => kv.Key, kv => new PropertyFieldBinding(kv.Value));
            this.fieldToPropertyMapping = prototype.fieldToPropertyMapping.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void AddBinding(string property, FieldInfo field, Type type)
        {
            PropertyFieldBinding propertyFieldBinding = new PropertyFieldBinding( property, field, type );
            this.propertyToFieldMapping.Add( property, propertyFieldBinding );

            List<string> propertyList;
            if ( !this.fieldToPropertyMapping.TryGetValue( propertyFieldBinding.Field.FieldName, out propertyList ) )
            {
                propertyList = new List<string>();
                this.fieldToPropertyMapping.Add(propertyFieldBinding.Field.FieldName, propertyList);
            }

            propertyList.Add( property );
        }

        public List<PropertyFieldBinding> GetDependentPropertiesBindings(string fieldName)
        {
            List<string> list;
            if (!this.fieldToPropertyMapping.TryGetValue(fieldName, out list))
            {
                return empty;
            }

            return list.Select(p => this.propertyToFieldMapping[p]).Where(d => d.IsActive).ToList();
        }

        public bool TryGetSourceFieldBinding(string property, out PropertyFieldBinding second)
        {
            return this.propertyToFieldMapping.TryGetValue(property, out second);
        }

        public void RuntimeInitialize()
        {
            foreach (PropertyFieldBinding dependency in this.propertyToFieldMapping.Values)
            {
                dependency.Field.RuntimeInitialize();
            }
        }

        public static PropertyFieldBindingsMap CreateFromPrototype(PropertyFieldBindingsMap prototype)
        {
            return new PropertyFieldBindingsMap(prototype);
        }
    }
}
