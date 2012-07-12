#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PostSharp.Toolkit.Domain
{
    internal static class CollectionHelpers
    {
        public static bool AddIfNew<T>( this IList<T> list, T item )
        {
            if ( !list.Contains( item ) )
            {
                list.Add( item );
                return true;
            }

            return false;
        }

        public static bool AddIfNew<TKey, TValue>(
            this ConditionalWeakTable<TKey, IList<TValue>> dictionary, TKey obj, TValue item, Func<IList<TValue>> listFactory = null ) where TKey : class
        {
            IList<TValue> list;
            TKey reference = obj;
            if ( !dictionary.TryGetValue( reference, out list ) )
            {
                list = listFactory == null ? new List<TValue>() : listFactory();
                dictionary.Add( reference, list );
            }

            return list.AddIfNew( item );
        }

        public static TValue GetOrCreate<TKey, TValue>( this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory )
        {
            TValue value;
            if ( !dictionary.TryGetValue( key, out value ) )
            {
                value = valueFactory();
                dictionary.Add( key, value );
            }

            return value;
        }

        public static void AddOrUpdate<TKey, TValue> (this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey( key ))
            {
                dictionary.Remove( key );
            }

            dictionary.Add( key, value );
        }
    }
}