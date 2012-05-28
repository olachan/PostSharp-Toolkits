#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PostSharp.Toolkit.Threading
{
    internal sealed class IdentityComparer<T> : IEqualityComparer<T>
    {
        public static readonly IdentityComparer<T> Instance = new IdentityComparer<T>();

        private IdentityComparer()
        {
        }

        public bool Equals( T x, T y )
        {
            return ReferenceEquals( x, y );
        }

        public int GetHashCode( T obj )
        {
            return RuntimeHelpers.GetHashCode( obj );
        }
    }
}