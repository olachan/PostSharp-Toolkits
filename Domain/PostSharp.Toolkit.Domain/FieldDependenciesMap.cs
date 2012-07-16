#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain
{
    internal static class FieldDependenciesMap
    {
        /// <summary>
        /// Dictionary with a list of dependent properties for each instrumented field
        /// </summary>
        public static Dictionary<string, List<string>> FieldDependentProperties { get; set; }
    }
}