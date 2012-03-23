using System;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using log4net;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Log4Net.Logging
{
    internal class LoggerMethodsBuilder
    {
        private readonly ModuleDeclaration module;
        private readonly ITypeSignature loggerType;

        private readonly Predicate<MethodDefDeclaration> objectPredicate;
        private readonly Predicate<MethodDefDeclaration> format1Predicate;
        private readonly Predicate<MethodDefDeclaration> format2Predicate;
        private readonly Predicate<MethodDefDeclaration> format3Predicate;
        private readonly Predicate<MethodDefDeclaration> formatArrayPredicate;
        private readonly Predicate<MethodDefDeclaration> objectExceptionPredicate;

        public LoggerMethodsBuilder(ModuleDeclaration module, ITypeSignature loggerType)
        {
            this.module = module;
            this.loggerType = loggerType;

            // matches XXX(string)
            this.objectPredicate = method => method.Parameters.Count == 1 &&
                                             IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.Object);

            // matches XXX(string format, object arg)
            this.format1Predicate = method => method.Parameters.Count == 2 &&
                                              IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                                              IntrinsicTypeSignature.Is(method.Parameters[1].ParameterType, IntrinsicType.Object);

            // matches XXX(string format, object arg0, object arg1)
            this.format2Predicate = method => method.Parameters.Count == 3 &&
                                              IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                                              IntrinsicTypeSignature.Is(method.Parameters[1].ParameterType, IntrinsicType.Object) &&
                                              IntrinsicTypeSignature.Is(method.Parameters[2].ParameterType, IntrinsicType.Object);

            // matches XXX(string format, object arg0, object arg1, object arg2)
            this.format3Predicate = method => method.Parameters.Count == 4 &&
                                              IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                                              IntrinsicTypeSignature.Is(method.Parameters[1].ParameterType, IntrinsicType.Object) &&
                                              IntrinsicTypeSignature.Is(method.Parameters[2].ParameterType, IntrinsicType.Object) &&
                                              IntrinsicTypeSignature.Is(method.Parameters[3].ParameterType, IntrinsicType.Object);

            // matches XXX(string format, params object[] args)
            this.formatArrayPredicate = method => method.Parameters.Count == 2 &&
                                                  IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.String) &&
                                                  method.Parameters[1].ParameterType.BelongsToClassification(TypeClassifications.Array);

            this.objectExceptionPredicate = method => method.Parameters.Count == 2 &&
                                                      IntrinsicTypeSignature.Is(method.Parameters[0].ParameterType, IntrinsicType.Object) &&
                                                      method.Parameters[1].ParameterType.MatchesReference(module.Cache.GetType(typeof(Exception)));

        }

        public LoggerMethods CreateLoggerMethods(string logLevel)
        {
            string isLoggingEnabledName = string.Format("get_Is{0}Enabled", logLevel);
            string writeObjectMethodName = logLevel;
            string writeFormatMethodName = logLevel + "Format";
            IMethod isloggingEnabledMethod = this.module.FindMethod(this.loggerType, isLoggingEnabledName);
            IMethod writeStringMethod = this.module.FindMethod(this.loggerType, writeObjectMethodName, this.objectPredicate);
            IMethod writeFormat1Method = this.module.FindMethod(this.loggerType, writeFormatMethodName, this.format1Predicate);
            IMethod writeFormat2Method = this.module.FindMethod(this.loggerType, writeFormatMethodName, this.format2Predicate);
            IMethod writeFormat3Method = this.module.FindMethod(this.loggerType, writeFormatMethodName, this.format3Predicate);
            IMethod writeFormatArrayMethod = this.module.FindMethod(this.loggerType, writeFormatMethodName, this.formatArrayPredicate);
            IMethod writeStringExceptionMethod = this.module.FindMethod(this.loggerType, writeObjectMethodName, this.objectExceptionPredicate);

            return new LoggerMethods(isloggingEnabledMethod, writeStringMethod, writeFormat1Method, writeFormat2Method,
                                     writeFormat3Method, writeFormatArrayMethod, writeStringExceptionMethod);
        }
    }
}