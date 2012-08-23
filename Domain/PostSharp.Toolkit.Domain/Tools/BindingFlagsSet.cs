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
        public static BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                                                BindingFlags.Static;

        public static BindingFlags AllInstanceDeclared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static BindingFlags PublicInstanceDeclared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        public static BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
    }
}