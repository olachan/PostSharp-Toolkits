#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Reflection;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    internal static class ReflectionHelper
    {
        public static bool IsInternalOrPublic( MethodInfo m, bool protectedIsPublic )
        {
            MethodAttributes access = m.Attributes & MethodAttributes.MemberAccessMask;
            switch ( access )
            {
                case MethodAttributes.Public:
                case MethodAttributes.Assembly:
                case MethodAttributes.FamORAssem:
                    return true;

                case MethodAttributes.FamANDAssem:
                case MethodAttributes.Family:
                    return protectedIsPublic;

                default:
                    return false;
            }
        }
    }
}