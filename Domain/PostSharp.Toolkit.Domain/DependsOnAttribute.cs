using System;
using System.Linq;

namespace PostSharp.Toolkit.Domain
{
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