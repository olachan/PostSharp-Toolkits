#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

namespace TestAssembly
{
    public class ThisArgumentTestClass
    {
        public void Method1()
        {
        }

        public int Method( string arg0 )
        {
            return arg0.GetHashCode();
        }

        public void LogThisArgument()
        {
        }
    }
}