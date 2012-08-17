#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

namespace PostSharp.Toolkit.Domain
{
    public sealed class ChildPropertyChangedEventArgs : EventArgs
    {
        public string Path { get; private set; }

        internal ChildPropertyChangedEventArgs( string propertyName, ChildPropertyChangedEventArgs parent = null )
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
}