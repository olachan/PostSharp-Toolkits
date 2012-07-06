using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PostSharp.Toolkit.INPC
{
    [Serializable]
    internal class PropertyDependencySerializationStore
    {
        private Dictionary<string, IList<string>> fieldDependentProperties;

        public PropertyDependencySerializationStore(PropertiesDependencieAnalyzer analyzer)
        {
            if (analyzer != null)
            {
                this.fieldDependentProperties = analyzer.FieldDependentProperties;
            }
        }

        public void CopyToMap()
        {
            if (this.fieldDependentProperties == null)
            {
                return;
            }

            // HACK: we don't know if OnDeserialized event was already fired on dictionary. The method is idempotent so it is not a problem it will fire more than once. 
            this.fieldDependentProperties.OnDeserialization( this );

            if (FieldDependenciesMap.FieldDependentProperties == null)
            {
                FieldDependenciesMap.FieldDependentProperties = new Dictionary<string, IList<string>>(this.fieldDependentProperties);
            }
            else
            {
                foreach (var fieldDependentProperty in this.fieldDependentProperties)
                {
                    FieldDependenciesMap.FieldDependentProperties.Add(fieldDependentProperty.Key, fieldDependentProperty.Value);
                }
            }

            this.fieldDependentProperties = null;
        }
    }
}
