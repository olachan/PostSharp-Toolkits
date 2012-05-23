#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Toolkit.Diagnostics.Weaver.Logging.Console;
using PostSharp.Toolkit.Diagnostics.Weaver.Logging.Trace;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    internal sealed class DiagnosticsBackendProvider : ILoggingBackendProvider
    {
        public ILoggingBackend GetBackend( string name )
        {
            switch ( name.ToLowerInvariant() )
            {
                case "console":
                    return new ConsoleBackend();
                case "trace":
                    return new TraceBackend();
                default:
                    return null;
            }
        }
    }
}