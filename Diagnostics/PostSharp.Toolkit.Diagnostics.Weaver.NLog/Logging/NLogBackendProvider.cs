#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using PostSharp.Toolkit.Diagnostics.Weaver.Logging;

namespace PostSharp.Toolkit.Diagnostics.Weaver.NLog.Logging
{
    public sealed class NLogBackendProvider : ILoggingBackendProvider
    {
        public ILoggingBackend GetBackend( string name )
        {
            if ( name.Equals( "nlog", StringComparison.OrdinalIgnoreCase ) )
                return new NLogBackend();

            return null;
        }
    }
}