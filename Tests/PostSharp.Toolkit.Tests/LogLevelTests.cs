﻿using NUnit.Framework;
using TestAssembly;

namespace PostSharp.Toolkit.Tests
{
    [TestFixture]
    public class LogLevelTests : ConsoleTestsFixture
    {
        [Test]
        public void LogLevel_DefaultMethod_MethodIsLoggedWithDebugLevel()
        {
            SimpleClass2 s = new SimpleClass2();
            s.NormalMethod();

            string output = OutputString.ToString();
            StringAssert.Contains("Debug|Entering: TestAssembly.SimpleClass2.NormalMethod()", output);
            StringAssert.Contains("Debug|Leaving: TestAssembly.SimpleClass2.NormalMethod()", output);
        }

        [Test]
        public void LogLevel_ErrorMethod_MethodIsLoggedWithErrorLevel()
        {
            SimpleClass2 s = new SimpleClass2();
            s.ErrorMethod();

            string output = OutputString.ToString();
            StringAssert.Contains("Error|Entering: TestAssembly.SimpleClass2.ErrorMethod()", output);
            StringAssert.Contains("Error|Leaving: TestAssembly.SimpleClass2.ErrorMethod()", output);
        }

        [Test]
        public void ParameterOptions_ThisParameter_ValueOfThisParameterIsPrinted()
        {
            SimpleClass s = new SimpleClass();
            s.LogThisArgument();

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.SimpleClass.LogThisArgument(this = TestAssembly.SimpleClass)", output);
        }
    }
}