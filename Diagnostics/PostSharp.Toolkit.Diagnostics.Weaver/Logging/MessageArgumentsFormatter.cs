using System;
using System.Runtime.Remoting.Contexts;
using System.Text;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    internal sealed class MessageArgumentsFormatter
    {
        public const int ThisArgumentPosition = -1;
        public const int ReturnParameterPosition = -2;

        private readonly MethodBodyTransformationContext context;
        private readonly StringBuilder formatBuilder = new StringBuilder();
        private readonly MethodDefDeclaration targetMethod;

        public MessageArgumentsFormatter(MethodBodyTransformationContext context)
        {
            this.context = context;
            this.targetMethod = context.TargetElement as MethodDefDeclaration;
            if (this.targetMethod == null)
            {
                throw new InvalidOperationException("Target element is not a method");
            }
        }

        public string CreateMessageArguments(LogOptions logOptions, out int[] argumentsIndex)
        {
            int arrayLength = this.GetArrayLength(logOptions);
            argumentsIndex = new int[arrayLength];
            
            this.formatBuilder.AppendFormat("{0}.{1}", this.targetMethod.DeclaringType, this.targetMethod.Name);
            this.formatBuilder.Append("(");

            int startParameter = 0;
            if ((logOptions & LogOptions.IncludeThisArgument) != 0 &&
                (this.context.MethodMapping.MethodSignature.CallingConvention & CallingConvention.HasThis) != 0)
            {
                this.formatBuilder.AppendFormat("this = ");
                AppendFormatPlaceholder(0, this.formatBuilder, this.targetMethod.DeclaringType);
                startParameter = 1;
                argumentsIndex[0] = ThisArgumentPosition;
            }

            int parameterCount = context.MethodMapping.MethodSignature.ParameterCount;
            for (int i = 0; i < parameterCount; i++)
            {
                if (i > 0)
                {
                    this.formatBuilder.Append(", ");
                }

                ITypeSignature parameterType = context.MethodMapping.MethodSignature.GetParameterType(i);
                if ((logOptions & LogOptions.IncludeParameterType) != 0)
                {
                    this.formatBuilder.Append(parameterType.ToString());
                }

                if ((logOptions & LogOptions.IncludeParameterName) != 0)
                {
                    this.formatBuilder.AppendFormat(" {0}", context.MethodMapping.MethodMappingInformation.GetParameterName(i));
                }

                if ((logOptions & LogOptions.IncludeParameterValue) != 0)
                {
                    this.formatBuilder.AppendFormat(" = ");

                    int index = i + startParameter;
                    AppendFormatPlaceholder(index, this.formatBuilder, parameterType);
                    argumentsIndex[index] = i;
                }
            }

            this.formatBuilder.Append(")");

            if (ShouldLogReturnType(logOptions))
            {
                this.formatBuilder.Append(" : ");
                AppendFormatPlaceholder(argumentsIndex.Length - 1, this.formatBuilder, this.targetMethod.ReturnParameter.ParameterType);
                argumentsIndex[argumentsIndex.Length - 1] = ReturnParameterPosition;
            }

            return this.formatBuilder.ToString();

        }

        private int GetArrayLength(LogOptions logOptions)
        {
            int result = 0;

            if ((logOptions & LogOptions.IncludeParameterValue) != 0)
            {
                result = this.context.MethodMapping.MethodSignature.ParameterCount;
            }

            if ((logOptions & LogOptions.IncludeThisArgument) != 0)
            {
                ++result;
            }

            if (ShouldLogReturnType(logOptions))
            {
                ++result;
            }

            return result;
        }

        private bool ShouldLogReturnType(LogOptions logOptions)
        {
            return (logOptions & LogOptions.IncludeReturnValue) != 0 &&
                   !IntrinsicTypeSignature.Is(this.context.MethodMapping.MethodSignature.ReturnType, IntrinsicType.Void);
        }

        private static void AppendFormatPlaceholder(int i, StringBuilder formatBuilder, ITypeSignature parameterType)
        {
            if (IntrinsicTypeSignature.Is(parameterType, IntrinsicType.String))
            {
                formatBuilder.AppendFormat("\"" + "{{{0}}}" + "\"", i);
            }
            else if (!parameterType.BelongsToClassification(TypeClassifications.Intrinsic) ||
                     IntrinsicTypeSignature.Is(parameterType, IntrinsicType.Object))
            {
                formatBuilder.Append("{{");
                formatBuilder.AppendFormat("{{{0}}}", i);
                formatBuilder.Append("}}");
            }
            else
            {
                formatBuilder.AppendFormat("{{{0}}}", i);
            }
        }
    }
}