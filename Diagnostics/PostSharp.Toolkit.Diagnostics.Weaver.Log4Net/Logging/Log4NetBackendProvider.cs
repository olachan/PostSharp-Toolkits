#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using PostSharp.Toolkit.Diagnostics.Weaver.Logging;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Log4Net.Logging
{
    internal sealed class Log4NetBackendProvider : ILoggingBackendProvider
    {
        public ILoggingBackend GetBackend( string name )
        {
            if ( name.Equals( "log4net", StringComparison.OrdinalIgnoreCase ) )
                return new Log4NetBackend();

            return null;
        }
    }
}