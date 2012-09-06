#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Reflection;

namespace PostSharp.Toolkit.Domain.Tools
{
    internal static class BindingFlagsSet
    {
        public const BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Static;

        public const BindingFlags AllInstanceDeclared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public const BindingFlags PublicInstanceDeclared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        public const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        public const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
    }
}