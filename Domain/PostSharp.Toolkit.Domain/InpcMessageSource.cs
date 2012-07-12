#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Domain
{
    internal static class InpcMessageSource
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
                               "Method {1} contains direct access to a field of another class." + 
                               "Use InstanceScopedProperty attribute to specify that property value depends only on state of current instance or DependsOn attribute to explicitly specify dependencies.";
                    case 2:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " +
                               "Method {1} contains call to non void (ref/out param) method of another class." +
                               "Use InstanceScopedProperty attribute to specify that property value depends only on state of current instance, DependsOn attribute to explicitly specify dependencies or mark called method with IdempotentMethodAttribute attribute to specify that the method is idempotent.";
                    case 3:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " + 
                               "Method {1} contains delegate call." +
                               "Use InstanceScopedProperty attribute to specify that property value depends only on state of current instance or DependsOn attribute to explicitly specify dependencies.";
                    default:
                        return null;
                }
            }
        }
    }
}