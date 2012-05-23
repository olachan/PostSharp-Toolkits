#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

namespace TestAssembly
{
    public static class StaticClass
    {
        public static void Method1()
        {
        }

        public static int GetNumber( int number )
        {
            return number;
        }

        public static DateTime GetDate( DateTime dt )
        {
            return dt;
        }
    }
}