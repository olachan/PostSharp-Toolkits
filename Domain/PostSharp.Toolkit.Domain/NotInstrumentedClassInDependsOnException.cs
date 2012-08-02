using System;

namespace PostSharp.Toolkit.Domain
{
    public class NotInstrumentedClassInDependsOnException : Exception
    {
        public NotInstrumentedClassInDependsOnException()
        {
        }

        public NotInstrumentedClassInDependsOnException( string message )
            : base( message )
        {
        }
    }
}