#region Copyright (c) 2012 by SharpCrafters s.r.o.

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
        private Dictionary<string, IList<string>> fieldDependentProperties;

        public PropertyDependencySerializationStore( PropertiesDependencieAnalyzer analyzer )
        {
            if ( analyzer != null )
            {
                this.fieldDependentProperties = analyzer.FieldDependentProperties;
            }
        }

        public void CopyToMap()
        {
            if ( this.fieldDependentProperties == null )
            {
                return;
            }

            // HACK: we don't know if OnDeserialized event was already fired on dictionary. The method is idempotent so it is not a problem it will fire more than once. 
            this.fieldDependentProperties.OnDeserialization( this );

            if ( FieldDependenciesMap.FieldDependentProperties == null )
            {
                FieldDependenciesMap.FieldDependentProperties = new Dictionary<string, IList<string>>( this.fieldDependentProperties );
            }
            else
            {
                foreach ( KeyValuePair<string, IList<string>> fieldDependentProperty in this.fieldDependentProperties )
                {
                    FieldDependenciesMap.FieldDependentProperties.Add( fieldDependentProperty.Key, fieldDependentProperty.Value );
                }
            }

            this.fieldDependentProperties = null;
        }
    }
}