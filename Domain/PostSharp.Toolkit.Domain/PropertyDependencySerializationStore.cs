﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// class used to store static data between compile time and runtime
    /// </summary>
    [Serializable]
    internal class PropertyDependencySerializationStore
    {
        //private Dictionary<Type, NotifyPropertyChangedAccessor.NotifyPropertyChangedTypeAccessors> notifyPropertyChangedTypeAccessors;

        private Dictionary<string, List<string>> fieldDependentProperties;

        public PropertyDependencySerializationStore(PropertiesDependencieAnalyzer analyzer)
        {
            //this.notifyPropertyChangedTypeAccessors = notifyPropertyChangedTypeAccessors;

            if ( analyzer != null )
            {
                this.fieldDependentProperties = analyzer.FieldDependentProperties;
            }
        }

        public void CopyToMap()
        {
            //this.DeserializeNotifyPropertyChangedAccessors();
            this.DeserializeFieldDependentProperties();
        }

        //private void DeserializeNotifyPropertyChangedAccessors()
        //{
        //    if ( this.notifyPropertyChangedTypeAccessors == null )
        //    {
        //        return;
        //    }

        //    this.notifyPropertyChangedTypeAccessors.OnDeserialization( this );

        //    NotifyPropertyChangedAccessor.SetAfterDeserialization( this.notifyPropertyChangedTypeAccessors );
        //}

        private void DeserializeFieldDependentProperties()
        {
            if ( this.fieldDependentProperties == null )
            {
                return;
            }

            // HACK: we don't know if OnDeserialized event was already fired on dictionary. The method is idempotent so it is not a problem it will fire more than once. 
            this.fieldDependentProperties.OnDeserialization( this );

            if ( FieldDependenciesMap.FieldDependentProperties == null )
            {
                FieldDependenciesMap.FieldDependentProperties = new Dictionary<string, List<string>>( this.fieldDependentProperties );
            }
            else
            {
                foreach ( KeyValuePair<string, List<string>> fieldDependentProperty in this.fieldDependentProperties )
                {
                    FieldDependenciesMap.FieldDependentProperties.Add( fieldDependentProperty.Key, fieldDependentProperty.Value );
                }
            }

            this.fieldDependentProperties = null;
        }
    }
}