using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PostSharp.Toolkit.INPC
{
    public static class CollectionHelpers
    {
        public static bool AddIfNew<T>(this IList<T> list , T item)
        {
            if (!list.Contains( item ))
            {
                list.Add( item );
                return true;
            }

            return false;
        }

        public static bool AddIfNew<T>(this ConditionalWeakTable<object, IList<T>> dictionary, object obj, T item, Func<IList<T>> listFactory = null)
        {
            IList<T> list;
            var reference = obj;
            if (!dictionary.TryGetValue( reference, out list ))
            {
                list = listFactory == null ? new List<T>() : listFactory();
                dictionary.Add( reference, list );
            }

            return list.AddIfNew( item );
        }

        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory )
        {
            TValue value;
            if(!dictionary.TryGetValue( key, out value ))
            {
                value = valueFactory();
                dictionary.Add( key, value );
            }

            return value;
        }
    }
}
