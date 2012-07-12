#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Domain
{
    internal static class DomainMessageSource
    {
        public static MessageSource Instance = new MessageSource( "PostSharp.Toolkit.INPC", new InpcMessageDispenser() );

        private class InpcMessageDispenser : MessageDispenser
        {
            public InpcMessageDispenser()
                : base( "INPC" )
            {
            }

            protected override string GetMessage( int number )
            {
                switch ( number )
                {
                    case 1:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " +
                               "Method {1} contains direct access to a field of another class." + "Use attribute TODO or TODO to TODO.";
                    case 2:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " +
                               "Method {1} contains call to non void (ref/out param) method of another class." + "Use attribute TODO or TODO to TODO.";
                    case 3:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " + "Method {1} contains delegate call." +
                               "Use attribute TODO or TODO to TODO.";
                    default:
                        return null;
                }
            }
        }
    }
}