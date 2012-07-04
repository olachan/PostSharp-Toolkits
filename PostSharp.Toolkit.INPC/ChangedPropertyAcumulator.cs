using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PostSharp.Toolkit.INPC
{
    internal static class ChangedPropertyAcumulator
    {


        [ThreadStatic]
        private static ConditionalWeakTable<object, IList<string>> changedProperties;

        public static ConditionalWeakTable<object, IList<string>> ChangedProperties
        {
            get
            {
                return changedProperties ?? (changedProperties = new ConditionalWeakTable<object, IList<string>>());
            }
        }

        [ThreadStatic]
        private static WeakHashSet changedObjects;

        public static WeakHashSet ChangedObjects
        {
            get
            {
                return changedObjects ?? (changedObjects = new WeakHashSet());
            }
        }

        public static void AddProperty(object obj, string propertyName)
        {
            if (ChangedProperties.AddIfNew( obj, propertyName ))
            {
                if (!ChangedObjects.Contains( obj ))
                {
                    ChangedObjects.Add( obj );
                }
            }
        }

        public static void Compact()
        {
            ChangedObjects.ClearNotAlive();
        }
    }
}
