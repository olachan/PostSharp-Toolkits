using System.Diagnostics;
using NUnit.Framework;
using TestAssembly;

namespace PostSharp.Toolkit.Tests.Trace
{
    [TestFixture]
    public class LogLevelTests : ConsoleTestsFixture
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            System.Diagnostics.Trace.Listeners.Add(new TextWriterTraceListener(TextWriter));
        }

        [Test]
        public void LogLevel_DefaultMethod_MethodIsLoggedWithDebugLevel()
        {
            SimpleClass2 s = new SimpleClass2();
            s.NormalMethod();

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: TestAssembly.SimpleClass2.NormalMethod()", output);
            StringAssert.Contains("Leaving: TestAssembly.SimpleClass2.NormalMethod()", output);
        }

        [Test]
        public void LogLevel_ErrorMethod_MethodIsLoggedWithErrorLevel()
        {
            SimpleClass2 s = new SimpleClass2();
            s.ErrorMethod();

            string output = OutputString.ToString();
            StringAssert.Contains("Error: 0 : Entering: TestAssembly.SimpleClass2.ErrorMethod()", output);
            StringAssert.Contains("Error: 0 : Leaving: TestAssembly.SimpleClass2.ErrorMethod()", output);
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