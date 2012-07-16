#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Map of dependencies built with <see cref="DependsOnAttribute"/>
    /// </summary>
    [Serializable]
    internal sealed class ExplicitDependencyMap
    {
        private readonly List<ExplicitDependency> dependencies;

        public ExplicitDependencyMap(IEnumerable<ExplicitDependency> dependencies)
        {
            this.dependencies = dependencies.ToList();
        }

        public IEnumerable<string> GetDependentProperties(string changedPath)
        {
            //TODO: Anything better than string matching possible?
            return this.dependencies.Where(d => d.Dependencies.Any(pd => pd.StartsWith(changedPath))).Select(d => d.PropertyName);
        }
    }

    [Serializable]
    internal sealed class ExplicitDependency
    {
        public ExplicitDependency(string propertyName, IEnumerable<string> dependencies)
        {
            this.PropertyName = propertyName;
            this.Dependencies = dependencies.ToList();
        }

        public string PropertyName { get; private set; }

        public List<string> Dependencies { get; private set; }
    }
}