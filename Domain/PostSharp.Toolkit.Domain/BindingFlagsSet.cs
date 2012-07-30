using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PostSharp.Toolkit.Domain
{
    internal static class BindingFlagsSet
    {
        public static BindingFlags AllMembers = BindingFlags.Public | 
                                                BindingFlags.NonPublic | 
                                                BindingFlags.Instance | 
                                                BindingFlags.FlattenHierarchy |
                                                BindingFlags.Static;

        public static BindingFlags AllInstanceDeclared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
    }
}
