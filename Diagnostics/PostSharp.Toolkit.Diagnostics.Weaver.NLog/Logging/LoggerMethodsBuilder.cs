#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;

namespace PostSharp.Toolkit.Diagnostics.Weaver.NLog.Logging
{
    internal class LoggerMethodsBuilder
    {
        private readonly ModuleDeclaration module;
        private readonly ITypeSignature loggerType;

        private readonly Predicate<MethodDefDeclaration> stringPredicate;
        private readonly Predicate<MethodDefDeclaration> format1Predicate;
        private readonly Predicate<MethodDefDeclaration> format2Predicate;
        private readonly Predicate<MethodDefDeclaration> format3Predicate;
        private readonly Predicate<MethodDefDeclaration> formatArrayPredicate;
        private readonly Predicate<MethodDefDeclaration> stringExceptionPredicate;

        public LoggerMethodsBuilder( ModuleDeclaration module, ITypeSignature loggerType )
        {
            this.module = module;
            this.loggerType = loggerType;

            // matches XXX(string)
            this.stringPredicate = method => method.Parameters.Count == 1 &&
                                             IntrinsicTypeSignature.Is( method.Parameters[0].ParameterType, IntrinsicType.String );

            // matches XXX(string format, object arg)
            this.format1Predicate = method => method.Parameters.Count == 2 &&
                                              IntrinsicTypeSignature.Is( method.Parameters[0].ParameterType, IntrinsicType.String ) &&
                                              IntrinsicTypeSignature.Is( method.Parameters[1].ParameterType, IntrinsicType.Object );

            // matches XXX(string format, object arg0, object arg1)
            this.format2Predicate = method => method.Parameters.Count == 3 &&
                                              IntrinsicTypeSignature.Is( method.Parameters[0].ParameterType, IntrinsicType.String ) &&
                                              IntrinsicTypeSignature.Is( method.Parameters[1].ParameterType, IntrinsicType.Object ) &&
                                              IntrinsicTypeSignature.Is( method.Parameters[2].ParameterType, IntrinsicType.Object );

            // matches XXX(string format, object arg0, object arg1, object arg2)
            this.format3Predicate = method => method.Parameters.Count == 4 &&
                                              IntrinsicTypeSignature.Is( method.Parameters[0].ParameterType, IntrinsicType.String ) &&
                                              IntrinsicTypeSignature.Is( method.Parameters[1].ParameterType, IntrinsicType.Object ) &&
                                              IntrinsicTypeSignature.Is( method.Parameters[2].ParameterType, IntrinsicType.Object ) &&
                                              IntrinsicTypeSignature.Is( method.Parameters[3].ParameterType, IntrinsicType.Object );

            // matches XXX(string format, params object[] args)
            this.formatArrayPredicate = method => method.Parameters.Count == 2 &&
                                                  IntrinsicTypeSignature.Is( method.Parameters[0].ParameterType, IntrinsicType.String ) &&
                                                  method.Parameters[1].ParameterType.BelongsToClassification( TypeClassifications.Array );

            this.stringExceptionPredicate = method => method.Parameters.Count == 2 &&
                                                      IntrinsicTypeSignature.Is( method.Parameters[0].ParameterType, IntrinsicType.String ) &&
                                                      method.Parameters[1].ParameterType.MatchesReference( module.Cache.GetType( typeof(Exception) ) );
        }

        public LoggerMethods CreateLoggerMethods( string logLevel )
        {
            string isLoggingEnabledName = string.Format( "get_Is{0}Enabled", logLevel );
            string writeStringMethodName = logLevel;
            string writeStringExceptionMethodName = logLevel + "Exception";
            IMethod isloggingEnabledMethod = this.module.FindMethod( this.loggerType, isLoggingEnabledName );
            IMethod writeStringMethod = this.module.FindMethod( this.loggerType, writeStringMethodName, this.stringPredicate );
            IMethod writeFormat1Method = this.module.FindMethod( this.loggerType, writeStringMethodName, this.format1Predicate );
            IMethod writeFormat2Method = this.module.FindMethod( this.loggerType, writeStringMethodName, this.format2Predicate );
            IMethod writeFormat3Method = this.module.FindMethod( this.loggerType, writeStringMethodName, this.format3Predicate );
            IMethod writeFormatArrayMethod = this.module.FindMethod( this.loggerType, writeStringMethodName, this.formatArrayPredicate );
            IMethod writeStringExceptionMethod = this.module.FindMethod( this.loggerType, writeStringExceptionMethodName, this.stringExceptionPredicate );

            return new LoggerMethods( isloggingEnabledMethod, writeStringMethod, writeFormat1Method, writeFormat2Method,
                                      writeFormat3Method, writeFormatArrayMethod, writeStringExceptionMethod );
        }
    }
}