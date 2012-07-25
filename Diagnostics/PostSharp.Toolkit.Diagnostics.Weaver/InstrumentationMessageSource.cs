#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Extensibility;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Toolkit.Diagnostics.Weaver
{
    internal static class InstrumentationMessageSource
    {
        public static readonly SdkMessageSource Instance = new SdkMessageSource( "PostSharp.Toolkit.Instrumentation", new InstrumentationMessageDispenser() );

        private class InstrumentationMessageDispenser : MessageDispenser
        {
            public InstrumentationMessageDispenser()
                : base( "DIA" )
            {
            }

            protected override string GetMessage( int number )
            {
                switch ( number )
                {
                    case 1:
                        return "Cannot find the logging backend '{0}'. Make sure the correct plug-in is installed.";

                    case 2:
                        return "Cannot apply the [Log] aspect to method '{0}' because it is abstract.";

                    default:
                        return null;
                }
            }
        }
    }
}