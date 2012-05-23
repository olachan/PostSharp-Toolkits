#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using NUnit.Framework;
using TestAssembly;

namespace PostSharp.Toolkit.Tests
{
    [TestFixture]
    public class MethodArgumentsTests : BaseTestsFixture
    {
        [Test]
        public void MethodArguments_MethodWith1Argument_LogsArgument()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith1Argument( "Test" );

            string output = OutputString.ToString();
            StringAssert.Contains( "TestAssembly.SimpleClass.MethodWith1Argument(string stringArg = \"Test\")", output );
        }

        [Test]
        public void MethodArguments_MethodWith2Arguments_LogsArgument()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith2Arguments( "Test", 42 );

            string output = OutputString.ToString();
            StringAssert.Contains( "TestAssembly.SimpleClass.MethodWith2Arguments(string stringArg = \"Test\", int intArg = 42)", output );
        }

        [Test]
        public void MethodArguments_MethodWith3Arguments_LogsArgument()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith3Arguments( "Test", 42, 128 );

            string output = OutputString.ToString();
            StringAssert.Contains( "TestAssembly.SimpleClass.MethodWith3Arguments(string stringArg = \"Test\", int intArg = 42, double doubleArg = 128)", output );
        }

        [Test]
        public void MethodArguments_MethodWith4Arguments_LogsArgument()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith4Arguments( "arg0", "arg1", "arg2", "arg3" );

            string output = OutputString.ToString();
            StringAssert.Contains( "TestAssembly.SimpleClass.MethodWith4Arguments(string arg0 = \"arg0\", " +
                                   "string arg1 = \"arg1\", string arg2 = \"arg2\", string arg3 = \"arg3\")", output );
        }
    }
}