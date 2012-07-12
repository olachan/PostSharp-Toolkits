using System;

namespace PostSharp.Toolkit.Domain
{
    public sealed class NotifyChildPropertyChangedEventArgs : EventArgs
    {
        public string Path { get; private set; }

        internal NotifyChildPropertyChangedEventArgs( string propertyName, NotifyChildPropertyChangedEventArgs parent = null )
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