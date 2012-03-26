using System.Text;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    public static class MessageFormatStringBuilder
    {
        public static string CreateMessageFormatString(LogOptions logOptions, MethodBodyTransformationContext context)
        {
            StringBuilder formatBuilder = new StringBuilder();

            MethodDefDeclaration targetMethod = context.TargetElement as MethodDefDeclaration;
            if (targetMethod == null)
            {
                return null;
            }

            formatBuilder.AppendFormat("{0}.{1}", targetMethod.DeclaringType, targetMethod.Name);
            formatBuilder.Append("(");

            int startParameter = 0;
            if ((logOptions & LogOptions.IncludeThisArgument) != 0)
            {
                if (context.MethodMapping.MethodSignature.CallingConvention == CallingConvention.HasThis)
                {
                    formatBuilder.AppendFormat("this = ");
                    AppendFormatPlaceholder(0, formatBuilder, targetMethod.DeclaringType);
                    startParameter = 1;
                }
            }

            int parameterCount = context.MethodMapping.MethodSignature.ParameterCount;
            for (int i = 0; i < parameterCount; i++)
            {
                if (i > 0)
                {
                    formatBuilder.Append(", ");
                }

                ITypeSignature parameterType = context.MethodMapping.MethodSignature.GetParameterType(i);
                if ((logOptions & LogOptions.IncludeParameterType) != 0)
                {
                    formatBuilder.Append(parameterType.ToString());
                    formatBuilder.Append(' ');
                }

                if ((logOptions & LogOptions.IncludeParameterName) != 0)
                {
                    formatBuilder.Append(context.MethodMapping.MethodMappingInformation.GetParameterName(i));
                    formatBuilder.Append(' ');
                }

                if ((logOptions & LogOptions.IncludeParameterValue) != 0)
                {
                    formatBuilder.AppendFormat("= ");

                    AppendFormatPlaceholder(i + startParameter, formatBuilder, parameterType);
                }
            }
            
            formatBuilder.Append(")");

            return formatBuilder.ToString();
        }

        private static void AppendFormatPlaceholder(int i, StringBuilder formatBuilder, ITypeSignature parameterType)
        {
            if (IntrinsicTypeSignature.Is(parameterType, IntrinsicType.String))
            {
                formatBuilder.AppendFormat("\"" + "{{{0}}}" + "\"", i);
            }
            else
            {
                formatBuilder.AppendFormat("{{{0}}}", i);
            }
        }
    }
}