#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Linq;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Custom attribute specifying explicit dependencies of marked property.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public class DependsOnAttribute : Attribute
    {
        public string[] Dependencies { get; private set; }

        public DependsOnAttribute( params string[] dependencies )
        {
            this.Dependencies = dependencies.ToArray();
        }
    }

    /// <summary>
    /// Class providing functionality of declaring explicit dependencies of properties.
    /// </summary>
    public static class Depends
    {
        // if(false) cant be used due to compiler optimizations
        public static readonly bool OnGuard = false;

        // params attribute cant be used because it emits array assignment to local variable not a direct call to method
        /// <summary>
        /// Specifies explicit dependency
        /// </summary>
        public static void On( object dependecie1 )
        {
        }

        public static void On( object dependecie1, object dependecie2 )
        {
        }

        public static void On( object dependecie1, object dependecie2, object dependecie3 )
        {
        }

        public static void On( object dependecie1, object dependecie2, object dependecie3, object dependecie4 )
        {
        }

        public static void On( object dependecie1, object dependecie2, object dependecie3, object dependecie4, object dependecie5 )
        {
        }

        public static void On( object dependecie1, object dependecie2, object dependecie3, object dependecie4, object dependecie5, object dependecie6 )
        {
        }

        public static void On(
            object dependecie1, object dependecie2, object dependecie3, object dependecie4, object dependecie5, object dependecie6, object dependecie7 )
        {
        }

        public static void On(
            object dependecie1,
            object dependecie2,
            object dependecie3,
            object dependecie4,
            object dependecie5,
            object dependecie6,
            object dependecie7,
            object dependecie8 )
        {
        }

        public static void On(
            object dependecie1,
            object dependecie2,
            object dependecie3,
            object dependecie4,
            object dependecie5,
            object dependecie6,
            object dependecie7,
            object dependecie8,
            object dependecie9 )
        {
        }

        public static void On(
            object dependecie1,
            object dependecie2,
            object dependecie3,
            object dependecie4,
            object dependecie5,
            object dependecie6,
            object dependecie7,
            object dependecie8,
            object dependecie9,
            object dependecie10 )
        {
        }
    }
}