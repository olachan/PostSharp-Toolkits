#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.PropertyDependencyAnalisys
{
    /// <summary>
    /// Map of dependencies built with <see cref="DependsOnAttribute"/> and <see cref="Depends.On"/> calls
    /// </summary>
    [Serializable]
    internal sealed class ExplicitDependencyMap
    {
        private List<ExplicitDependency> dependencies;

        public ExplicitDependencyMap( IEnumerable<ExplicitDependency> dependencies )
        {
            this.dependencies = dependencies.ToList();
        }

        public ExplicitDependencyMap AddDependency( string propertyName, string invocationPath )
        {
            ExplicitDependency dependency = this.dependencies.FirstOrDefault( d => d.PropertyName == propertyName );

            if ( dependency == null )
            {
                dependency = new ExplicitDependency( propertyName, new[] { invocationPath } );
                this.dependencies.Add( dependency );
            }
            else if ( !dependency.Dependencies.Any( p => p.StartsWith( invocationPath ) ) )
            {
                dependency.Dependencies.Add( invocationPath );
            }

            return this;
        }

        public ExplicitDependencyMap AddDependencies( string propertyName, IEnumerable<string> invocationPaths )
        {
            foreach ( string invocationPath in invocationPaths )
            {
                this.AddDependency( propertyName, invocationPath );
            }

            return this;
        }

        public IEnumerable<string> GetDependentProperties( string changedPath )
        {
            //TODO: Anything better than string matching possible?
            return this.dependencies.Where( d => d.Dependencies.Any( pd => pd.StartsWith( changedPath ) ) ).Select( d => d.PropertyName );
        }
    }

    [Serializable]
    internal sealed class ExplicitDependency
    {
        public ExplicitDependency( string propertyName, IEnumerable<string> dependencies )
        {
            this.PropertyName = propertyName;
            this.Dependencies = dependencies.ToList();
        }

        public string PropertyName { get; private set; }

        public List<string> Dependencies { get; private set; }
    }
}