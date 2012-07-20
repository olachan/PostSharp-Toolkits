#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Linq;
using System.Reflection;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Builder for <see cref="ExplicitDependencyMap"/> 
    /// </summary>
    internal sealed class ExplicitDependencyAnalyzer
    {
        public static ExplicitDependencyMap Analyze(Type type)
        {
            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var properties = allProperties
                .Where(p => !p.GetCustomAttributes(typeof(NotifyPropertyChangedIgnoreAttribute), true).Any())
                .Select(p => new { Property = p, DependsOn = p.GetCustomAttributes(typeof(DependsOnAttribute), false) })
                .Where(p => p.DependsOn.Any());

            return new ExplicitDependencyMap(properties.Select(p => new ExplicitDependency(p.Property.Name, p.DependsOn.SelectMany(d => ((DependsOnAttribute)d).Dependencies))));
        }
    }
}