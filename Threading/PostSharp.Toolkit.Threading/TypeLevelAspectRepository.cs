#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading
{
    internal sealed class TypeLevelAspectRepository : IAspect
    {
        private readonly HashSet<Type> aspects = new HashSet<Type>();

        public IEnumerable<AspectInstance> GetAspect( Type type, Func<Type, AspectInstance> createAspect )
        {
            Assembly currentAssembly = type.Assembly;
            Type rootType = type;

            for ( ;; )
            {
                if ( rootType.BaseType != null && rootType.BaseType.Assembly == currentAssembly )
                    rootType = rootType.BaseType;
                else break;
            }

            if ( !this.aspects.Contains( rootType ) )
            {
                this.aspects.Add( rootType );

                yield return createAspect( rootType );
            }
        }
    }
}