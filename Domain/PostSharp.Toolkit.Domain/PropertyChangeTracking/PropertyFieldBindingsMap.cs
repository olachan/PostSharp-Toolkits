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

using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.PropertyChangeTracking
{
    /// <summary>
    /// Storage for <see cref="PropertyFieldBinding"/>s
    /// Two way field to property, one to many mapping. 
    /// </summary>
    [Serializable]
    internal sealed class PropertyFieldBindingsMap
    {
        private static readonly List<PropertyFieldBinding> empty = new List<PropertyFieldBinding>( 0 );

        // runtime modified
        // invariant : at most one binding is avtive.
        private readonly Dictionary<string, List<PropertyFieldBinding>> propertyToFieldMapping;

        // runtime static
        private readonly Dictionary<string, List<string>> fieldToPropertyMapping;

        // runtime static
        private readonly Dictionary<string, FieldInfoWithCompiledGetter> filedInfos;

        // runtime static
        private readonly FieldValueComparer fieldValueComparer;

        public PropertyFieldBindingsMap( Type type, FieldValueComparer fieldValueComparer )
        {
            this.propertyToFieldMapping = new Dictionary<string, List<PropertyFieldBinding>>();
            this.fieldToPropertyMapping = new Dictionary<string, List<string>>();
            this.fieldValueComparer = fieldValueComparer;

            this.filedInfos = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ).ToDictionary(
                f => f.Name, f => new FieldInfoWithCompiledGetter( f, type ) );
        }

        private PropertyFieldBindingsMap( PropertyFieldBindingsMap prototype )
        {
            this.propertyToFieldMapping = prototype.propertyToFieldMapping.ToDictionary(
                kv => kv.Key, kv => kv.Value.Select( b => new PropertyFieldBinding( b ) ).ToList() );
            this.fieldValueComparer = prototype.fieldValueComparer;
            this.fieldToPropertyMapping = prototype.fieldToPropertyMapping;
            this.filedInfos = prototype.FiledInfos;
        }

        public Dictionary<string, FieldInfoWithCompiledGetter> FiledInfos
        {
            get
            {
                return this.filedInfos;
            }
        }

        public void AddBindings( PropertyInfo propertyInfo, List<FieldInfo> fieldList, Type type )
        {
            bool isActive = fieldList.Count == 1;
            string propertyName = propertyInfo.Name;

            if ( fieldList.Count > 5 )
            {
                DomainMessageSource.Instance.Write( propertyInfo, SeverityType.Warning, "INPC007", propertyInfo.Name );
            }

            foreach ( FieldInfo field in fieldList )
            {
                if (field.FieldType.ContainsGenericParameters)
                {
                    DomainMessageSource.Instance.Write(propertyInfo, SeverityType.Error, "INPC014", propertyInfo.Name, field.Name);
                }

                this.propertyToFieldMapping.AddToListValue( propertyName, new PropertyFieldBinding( propertyName, this.FiledInfos[field.Name], isActive ) );
                this.fieldToPropertyMapping.AddToListValue( field.Name, propertyName );
            }
        }

        public List<PropertyFieldBinding> GetDependentPropertiesBindings( string fieldName )
        {
            List<string> list;
            if ( !this.fieldToPropertyMapping.TryGetValue( fieldName, out list ) )
            {
                return empty;
            }

            return list.SelectMany( p => this.propertyToFieldMapping[p] ).Where( d => d.IsActive ).ToList();
        }

        public bool TryGetSourceFieldBinding( string property, out PropertyFieldBinding binding )
        {
            List<PropertyFieldBinding> bindings;
            if ( !this.propertyToFieldMapping.TryGetValue( property, out bindings ) )
            {
                binding = null;
                return false;
            }

            binding = bindings.SingleOrDefault( b => b.IsActive );

            return binding != null;
        }

        public bool TryGetSourceFieldBindings( string property, out List<PropertyFieldBinding> bindings )
        {
            return this.propertyToFieldMapping.TryGetValue( property, out bindings );
        }

        /// <summary>
        /// Finds a field that holds object returned by the property. Activates binding to that field and deactivates other bindings.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>true if there is active binding, false if not.</returns>
        public bool RefreshPropertyBindings( LocationInterceptionArgs args )
        {
            bool bindingFound = false;
            PropertyFieldBinding bindingToActivate = null;
            List<PropertyFieldBinding> sourceFields;
            if ( this.TryGetSourceFieldBindings( args.LocationName, out sourceFields ) )
            {
                foreach ( PropertyFieldBinding propertyFieldBinding in sourceFields.OrderBy( f => f.IsActive ) )
                {
                    object value = propertyFieldBinding.Field.GetValue( args.Instance );

                    if ( this.fieldValueComparer.AreEqual( args.LocationFullName, value, args.Value ) )
                    {
                        // field and property values are equal, mark source field binding as active and if there is a handler hooked to the property un hook it
                        bindingFound = true;
                        if ( propertyFieldBinding.IsActive )
                        {
                            break;
                        }

                        bindingToActivate = propertyFieldBinding;
                    }
                    else
                    {
                        // field and property values differ, mark source field binding as inactive and re hook a handler to the property
                        propertyFieldBinding.IsActive = false;
                    }
                }
            }

            if ( bindingFound && bindingToActivate != null )
            {
                bindingToActivate.IsActive = true;
            }

            return bindingFound;
        }

        public void RuntimeInitialize()
        {
            foreach ( FieldInfoWithCompiledGetter fieldInfo in this.FiledInfos.Values )
            {
                fieldInfo.RuntimeInitialize();
            }
        }

        public static PropertyFieldBindingsMap CreateFromPrototype( PropertyFieldBindingsMap prototype )
        {
            return new PropertyFieldBindingsMap( prototype );
        }
    }
}