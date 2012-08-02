using System;
using System.Linq;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Custom attribute specifying explicit dependencies of marked property.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public class DependsOnAttribute : Attribute
    {
        public string[] Dependencies { get; private set; }

        public DependsOnAttribute( params string[] dependencies )
        {
            this.Dependencies = dependencies.ToArray();
        }
    }
}