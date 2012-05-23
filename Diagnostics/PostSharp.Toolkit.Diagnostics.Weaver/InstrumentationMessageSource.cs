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
        public static SdkMessageSource Instance = new SdkMessageSource( "PostSharp.Toolkit.Instrumentation", new InstrumentationMessageDispenser() );

        private class InstrumentationMessageDispenser : MessageDispenser
        {
            public InstrumentationMessageDispenser()
                : base( "IN" )
            {
            }

            protected override string GetMessage( int number )
            {
                switch ( number )
                {
                    case 1:
                        return "Cannot find the logging backend '{0}'. Make sure the correct plug-in is installed.";

                    default:
                        return null;
                }
            }
        }
    }
}