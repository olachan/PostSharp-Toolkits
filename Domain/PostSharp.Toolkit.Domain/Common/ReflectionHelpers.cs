#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Linq;
using System.Reflection;

using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain.Common
{
    internal static class ReflectionHelpers
    {
      

        /// <summary>
        /// Uses ReflectionSearch so can be used only in Compile Time !!!
        /// </summary>
        public static bool IsToBeImplementedMethod(this MethodBase method)
        {
            var usedDeclarations = ReflectionSearch.GetDeclarationsUsedByMethod(method);

            if (usedDeclarations.Count() != 1)
            {
                return false;
            }

            var usedDeclaration = usedDeclarations.Single();

            return usedDeclaration.UsedType == typeof(ToBeIntroducedException) && usedDeclaration.UsedDeclaration is ConstructorInfo;
        }

        public static string FullName( this MemberInfo memberInfo )
        {
            return memberInfo.DeclaringType == null ? memberInfo.Name : string.Format( "{0}.{1}", memberInfo.DeclaringType.FullName, memberInfo.Name );
        }

        public static bool IsEventAccessor(this MethodInfo methodInfo)
        {
            return methodInfo.IsSpecialName && (methodInfo.Name.StartsWith( "add_" ) || methodInfo.Name.StartsWith( "remove_" ));
        }

        public static bool IsPropertyAccessor(this MethodInfo methodInfo)
        {
            return methodInfo.IsSpecialName && (methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_"));
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

        public static TAttribute[] GetCustomAttributeFromMethodOrProperty<TAttribute>(this MethodInfo methodInfo, bool inherit)
            where TAttribute : Attribute
        {
            PropertyInfo propertyInfo;

            object[] attributes = methodInfo.GetCustomAttributes( typeof(TAttribute), inherit );
            if (attributes.Any())
            {
                return attributes.Cast<TAttribute>().ToArray();
            }

            if ((propertyInfo = methodInfo.GetAccessorsProperty()) != null)
            {
                return propertyInfo.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().ToArray();
            }

            return new TAttribute[0];
        }
    }
}