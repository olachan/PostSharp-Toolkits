#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Linq;
using System.Reflection;

namespace PostSharp.Toolkit.Domain.Common
{
    internal static class ReflectionHelpers
    {
        public static bool IsFrameworkStaticMethod( this MethodBase method )
        {
            if ( !method.IsStatic || method.DeclaringType == null )
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

        public static string FullName( this MemberInfo memberInfo )
        {
            return memberInfo.DeclaringType == null ? memberInfo.Name : string.Format( "{0}.{1}", memberInfo.DeclaringType.FullName, memberInfo.Name );
        }

        public static bool IsEventAccessor(this MethodInfo methodInfo)
        {
            return methodInfo.IsSpecialName && !methodInfo.Name.StartsWith( "add_" ) && !methodInfo.Name.StartsWith( "remove_" );
        }

        public static bool IsPropertyAccessor(this MethodInfo methodInfo)
        {
            return methodInfo.IsSpecialName && !methodInfo.Name.StartsWith("get_") && !methodInfo.Name.StartsWith("set_");
        }

        public static PropertyInfo GetAccessorsProperty(this MethodInfo methodInfo)
        {
            if (!methodInfo.IsPropertyAccessor())
            {
                return null;
            }

            Type declaringType = methodInfo.DeclaringType;

            return declaringType.GetProperty(methodInfo.Name.Remove(0, 4), BindingFlagsSet.AllInstance);
        }

        public static bool IsDefinedOnMethodOrProperty(this MethodInfo methodInfo, Type attributeType, bool inherit)
        {
            PropertyInfo propertyInfo;

            if (methodInfo.IsDefined(attributeType, inherit))
            {
                return true;
            }

            if ((propertyInfo = methodInfo.GetAccessorsProperty()) != null)
            {
                return propertyInfo.IsDefined( attributeType, inherit );
            }

            return false;
        }

        public static bool IsIdempotentMethod( this MethodBase method )
        {
            return method.GetCustomAttributes( typeof(IdempotentMethodAttribute), false ).Any();
        }

        public static bool IsInpcIgnoredMethod( this MethodBase method )
        {
            return method.GetCustomAttributes( typeof(NotifyPropertyChangedIgnoreAttribute), false ).Any();
        }

        public static bool IsVoidNoRefOut( this MethodInfo methodInfo )
        {
            return (methodInfo.ReturnType == typeof(void) && !methodInfo.GetParameters().Any( p => p.ParameterType.IsByRef ));
        }

        public static bool IsObjectToString( this MethodBase methodInfo )
        {
            return (methodInfo.Name == "ToString" && methodInfo.DeclaringType == typeof(object));
        }

        public static bool IsObjectGetHashCode( this MethodBase methodInfo )
        {
            return (methodInfo.Name == "GetHashCode" && methodInfo.DeclaringType == typeof(object));
        }

        public static bool IsIntrinsicOrObjectArray( this Type type )
        {
            return type.IsArray && (type.GetElementType().IsIntrinsic() || type.GetElementType() == typeof(object));
        }

        public static bool IsIntrinsic( this Type type )
        {
            return type.IsPrimitive || type == typeof(string);
        }

        public static bool HasOnlyIntrinsicOrObjectParameters( this MethodBase methodInfo )
        {
            return
                methodInfo.GetParameters().All(
                    p => p.ParameterType.IsIntrinsic() || p.ParameterType.IsIntrinsicOrObjectArray() || p.ParameterType == typeof(object) );
        }
    }
}