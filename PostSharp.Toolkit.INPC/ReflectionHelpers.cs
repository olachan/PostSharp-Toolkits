#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Linq;
using System.Reflection;

namespace PostSharp.Toolkit.INPC
{
    internal static class ReflectionHelpers
    {
        public static bool IsFrameworkStaticMethod( this MethodBase method )
        {
            // TODO verify validity of this approach
            if ( !method.IsStatic )
            {
                return false;
            }

            AssemblyProductAttribute attribute =
                method.DeclaringType.Assembly.GetCustomAttributes( typeof(AssemblyProductAttribute), false ).FirstOrDefault() as AssemblyProductAttribute;

            if ( attribute == null )
            {
                return false;
            }

            return attribute.Product == "Microsoft® .NET Framework";
        }

        public static bool IsStateIndependentMethod( this MethodBase method )
        {
            return method.GetCustomAttributes( typeof(IdempotentMethodAttribute), false ).Any();
        }

        public static bool IsVoidNoRefOut( this MethodInfo methodInfo )
        {
            return (methodInfo.ReturnType == typeof(void) && !methodInfo.GetParameters().Any( p => p.ParameterType.IsByRef ));
        }
    }
}