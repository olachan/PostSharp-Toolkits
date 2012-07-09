#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Linq;

namespace PostSharp.Toolkit.INPC
{
    public interface IPropagatedChange
    {
        void RaisePropagatedChange( PropagatedChangeEventArgs args );

        event PropagatedChangeEventHandler PropagatedChange;
    }

    public delegate void PropagatedChangeEventHandler( object sender, PropagatedChangeEventArgs args );

    public class PropagatedChangeEventArgs : EventArgs
    {
        public string Path { get; private set; }

        public PropagatedChangeEventArgs( string propertyName, PropagatedChangeEventArgs parent = null )
        {
            if ( parent != null )
            {
                this.Path = string.Format( "{0}.{1}", propertyName, parent.Path );
            }
            else
            {
                this.Path = propertyName;
            }
        }
    }

    [AttributeUsage( AttributeTargets.Property )]
    public class DependsOn : Attribute
    {
        public string[] Dependencies { get; private set; }

        public DependsOn( params string[] dependencies )
        {
            this.Dependencies = dependencies.ToArray();
        }
    }
}