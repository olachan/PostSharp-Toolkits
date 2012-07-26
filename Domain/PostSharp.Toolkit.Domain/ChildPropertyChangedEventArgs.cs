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