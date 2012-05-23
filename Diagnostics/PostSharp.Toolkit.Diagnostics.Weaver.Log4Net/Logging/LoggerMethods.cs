#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Sdk.CodeModel;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Log4Net.Logging
{
    internal class LoggerMethods
    {
        public IMethod IsLoggingEnabledMethod { get; private set; }
        public IMethod WriteStringMethod { get; private set; }
        public IMethod WriteStringFormat1Method { get; private set; }
        public IMethod WriteStringFormat2Method { get; private set; }
        public IMethod WriteStringFormat3Method { get; private set; }
        public IMethod WriteStringFormatArrayMethod { get; private set; }
        public IMethod WriteStringExceptionMethod { get; private set; }

        public LoggerMethods( IMethod isLoggingEnabledMethod, IMethod writeStringMethod, IMethod writeStringFormat1Method,
                              IMethod writeStringFormat2Method, IMethod writeStringFormat3Method,
                              IMethod writeStringFormatArrayMethod, IMethod writeStringExceptionMethod )
        {
            this.IsLoggingEnabledMethod = isLoggingEnabledMethod;
            this.WriteStringMethod = writeStringMethod;
            this.WriteStringFormat1Method = writeStringFormat1Method;
            this.WriteStringFormat2Method = writeStringFormat2Method;
            this.WriteStringFormat3Method = writeStringFormat3Method;
            this.WriteStringFormatArrayMethod = writeStringFormatArrayMethod;
            this.WriteStringExceptionMethod = writeStringExceptionMethod;
        }
    }
}