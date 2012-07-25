using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain
{
    // Two way field to property, one to many mapping. 
    [Serializable]
    internal sealed class PropertyToFieldBiDirectionalBinding
    {
        private static readonly List<FieldValueBinding> empty = new List<FieldValueBinding>(0);

        //runtime modified
        private readonly Dictionary<string, List<FieldValueBinding>> propertyToFieldMapping;
        
        // runtime static
        private readonly Dictionary<string, List<string>> fieldToPropertyMapping;

        // runtime static
        private readonly Dictionary<string, FieldInfoWithCompiledGetter> filedInfos;

        // runtime static
        private readonly FieldValueComparer fieldValueComparer;

        public PropertyToFieldBiDirectionalBinding(Type type, FieldValueComparer fieldValueComparer)
        {
            this.propertyToFieldMapping = new Dictionary<string, List<FieldValueBinding>>();
            this.fieldToPropertyMapping = new Dictionary<string, List<string>>();
            this.fieldValueComparer = fieldValueComparer;

            this.filedInfos = type
                .GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )
                .ToDictionary( f => f.Name, f => new FieldInfoWithCompiledGetter( f, type ) );
        }

        private PropertyToFieldBiDirectionalBinding(PropertyToFieldBiDirectionalBinding prototype)
        {
            this.propertyToFieldMapping = prototype.propertyToFieldMapping
                .ToDictionary(kv => kv.Key, kv => kv.Value.Select( b => new FieldValueBinding( b ) ).ToList());
            this.fieldValueComparer = prototype.fieldValueComparer;
            this.fieldToPropertyMapping = prototype.fieldToPropertyMapping;
            this.filedInfos = prototype.filedInfos;
        }

        public void AddBindings(PropertyInfo propertyInfo, List<FieldInfo> fieldList, Type type)
        {
            bool isActive = fieldList.Count == 1;
            string propertyName = propertyInfo.Name;

            if (fieldList.Count > 5)
            {
                DomainMessageSource.Instance.Write( propertyInfo, SeverityType.Warning, "INPC007", propertyInfo.Name );
            }

            foreach ( FieldInfo field in fieldList )
            {
                FieldValueBinding fieldValueBinding = new FieldValueBinding(propertyName, filedInfos[field.Name], isActive);

                List<FieldValueBinding> fieldValueBindings;
                if (!this.propertyToFieldMapping.TryGetValue(propertyName, out fieldValueBindings))
                {
                    fieldValueBindings = new List<FieldValueBinding>();
                    this.propertyToFieldMapping.Add(propertyName, fieldValueBindings);
                }

                fieldValueBindings.Add (fieldValueBinding);

                List<string> propertyList;
                if (!this.fieldToPropertyMapping.TryGetValue(fieldValueBinding.Field.FieldName, out propertyList))
                {
                    propertyList = new List<string>();
                    this.fieldToPropertyMapping.Add(fieldValueBinding.Field.FieldName, propertyList);
                }

                propertyList.Add(propertyName);
            }
        }

        public List<FieldValueBinding> GetDependentPropertiesBindings(string fieldName)
        {
            List<string> list;
            if (!this.fieldToPropertyMapping.TryGetValue(fieldName, out list))
            {
                return empty;
            }

            return list.SelectMany(p => this.propertyToFieldMapping[p])
                .Where(d => d.IsActive)
                .ToList();
        }

        public bool TryGetSourceFieldBinding(string property, out FieldValueBinding binding)
        {
            List<FieldValueBinding> bindings;
            if (!this.propertyToFieldMapping.TryGetValue(property, out bindings))
            {
                binding = null;
                return false;
            }

            binding = bindings.SingleOrDefault( b => b.IsActive );

            return binding != null;
        }

        public bool TryGetSourceFieldBindings(string property, out List<FieldValueBinding> bindings)
        {
            return this.propertyToFieldMapping.TryGetValue( property, out bindings );
        }

        public bool RefreshPropertyBindings(LocationInterceptionArgs args)
        {
            bool bindingFound = false;
            List<FieldValueBinding> sourceFields;
            if (this.TryGetSourceFieldBindings(args.LocationName, out sourceFields))
            {
                foreach (FieldValueBinding fieldValueBinding in sourceFields.OrderBy(f => f.IsActive))
                {
                    object value = fieldValueBinding.Field.GetValue(args.Instance);

                    if (this.fieldValueComparer.AreEqual(args.LocationFullName, value, args.Value))
                    {
                        // field and property values are equal, mark source field binding as active and if there is a handler hooked to the property un hook it
                        bindingFound = true;
                        if (fieldValueBinding.IsActive)
                        {
                            break;
                        }

                        fieldValueBinding.IsActive = true;
                    }
                    else
                    {
                        // field and property values differ, mark source field binding as inactive and re hook a handler to the property
                        fieldValueBinding.IsActive = false;
                    }
                }
            }

            return bindingFound;
        }

        public void RuntimeInitialize()
        {
            foreach (FieldInfoWithCompiledGetter fieldInfo in filedInfos.Values)
            {
                fieldInfo.RuntimeInitialize();
            }
        }

        public static PropertyToFieldBiDirectionalBinding CreateFromPrototype(PropertyToFieldBiDirectionalBinding prototype)
        {
            return new PropertyToFieldBiDirectionalBinding(prototype);
        }
    }
}
