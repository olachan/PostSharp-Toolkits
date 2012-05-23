#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Text;
using NUnit.Framework;

namespace PostSharp.Toolkit.Tests
{
    [TestFixture]
    public class MethodInterceptionTest : BaseTestsFixture
    {
        [Test]
        public void Interception_MethodWithOneArgument_PrintsArgumentValue()
        {
            MethodInterception t = new MethodInterception();
            t.Method1( "Test" );

            string output = OutputString.ToString();
            StringAssert.Contains( "MethodInterception.Method1(string arg = \"Test\")", output );
        }

        [Test]
        public void Interception_LastArgumentIsTheResult_LogsReturnValue()
        {
            MethodInterception t = new MethodInterception();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append( "Test" );

            t.Method6( "Test", new object(), 3, DateTime.Now, DateTime.Now.AddYears( 1 ), stringBuilder );

            string output = OutputString.ToString();
            StringAssert.Contains( "Leaving: PostSharp.Toolkit.Tests.MethodInterception.Method6() : {Test}", output );
        }
    }
}