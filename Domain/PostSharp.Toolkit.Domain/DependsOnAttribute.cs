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
    //[AttributeUsage( AttributeTargets.Property )]
    //public class DependsOnAttribute : Attribute
    //{
    //    public string[] Dependencies { get; private set; }

    //    public DependsOnAttribute( params string[] dependencies )
    //    {
    //        this.Dependencies = dependencies.ToArray();
    //    }
    //}

    /// <summary>
    /// Class providing functionality of declaring explicit dependencies of properties.
    /// </summary>
    public static class Depends
    {
        // if(false) cannot be used explicitly due to compiler optimizations
        public static readonly bool Guard = false;

        // params attribute cannot be used because it emits array assignment to local variable not a direct call to method
        /// <summary>
        /// Specifies explicit dependency
        /// </summary>
        public static void On( object dependency1 )
        {
        }

        public static void On( object dependency1, object dependency2 )
        {
        }

        public static void On( object dependency1, object dependency2, object dependency3 )
        {
        }

        public static void On( object dependency1, object dependency2, object dependency3, object dependency4 )
        {
        }

        public static void On( object dependency1, object dependency2, object dependency3, object dependency4, object dependency5 )
        {
        }

        public static void On( object dependency1, object dependency2, object dependency3, object dependency4, object dependency5, object dependency6 )
        {
        }

        public static void On(
            object dependency1, object dependency2, object dependency3, object dependency4, object dependency5, object dependency6, object dependency7 )
        {
        }

        public static void On(
            object dependency1,
            object dependency2,
            object dependency3,
            object dependency4,
            object dependency5,
            object dependency6,
            object dependency7,
            object dependency8 )
        {
        }

        public static void On(
            object dependency1,
            object dependency2,
            object dependency3,
            object dependency4,
            object dependency5,
            object dependency6,
            object dependency7,
            object dependency8,
            object dependency9 )
        {
        }

        public static void On(
            object dependency1,
            object dependency2,
            object dependency3,
            object dependency4,
            object dependency5,
            object dependency6,
            object dependency7,
            object dependency8,
            object dependency9,
            object dependency10 )
        {
        }
    }
}