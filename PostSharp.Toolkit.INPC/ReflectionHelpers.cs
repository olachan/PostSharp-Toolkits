using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PostSharp.Toolkit.INPC
{
    internal static class ReflectionHelpers
    {
        public static bool IsFrameworkStaticMethod(this MethodBase method)
        {
            if (!method.IsStatic)
            {
                return false;
            }

            AssemblyProductAttribute attribute = method.DeclaringType.Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false).FirstOrDefault() as AssemblyProductAttribute;

            if (attribute == null)
            {
                return false;
            }

            return attribute.Product == "Microsoft® .NET Framework";
        }

        public static bool IsStateIndependentMethod(this MethodBase method)
        {
            return method.GetCustomAttributes( typeof(StateIndependentMethod), false ).Any();
        }

        public static bool IsVoidNoRefOut(this MethodInfo methodInfo)
        {
            return (methodInfo.ReturnType == typeof(void) && !methodInfo.GetParameters().Any( p => p.ParameterType.IsByRef ));
        }
    }
}
