#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using TestAssembly;

namespace PostSharp.Toolkit.Tests
{
    [TestFixture]
    public class StaticTests : BaseTestsFixture
    {
        [Test]
        public void StaticClass_MethodWithReturnValue_PrintsReturnValue()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            int number = StaticClass.GetNumber( 42 );

            

            string output = OutputString.ToString();
            StringAssert.Contains( "TestAssembly.StaticClass.GetNumber(int number = 42)", output );
            StringAssert.Contains( "TestAssembly.StaticClass.GetNumber() : 42", output );
        }

        [Test]
        public void StaticClass_MethodReturningStruct_ValueIsPrinted()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;


            DateTime dt = new DateTime( 1970, 1, 1 );
            StaticClass.GetDate( dt );

         
            string output = OutputString.ToString();
            StringAssert.Contains( "1970", output );
        }
    }
}