using NUnit.Framework;
using TestAssembly;
using log4net.Config;

namespace PostSharp.Toolkit.Tests.Log4Net
{
    [TestFixture]
    public class LogLevelTests : ConsoleTestsFixture
    {
        [SetUp]
        public override void SetUp()
        {
            BasicConfigurator.Configure();
            base.SetUp();
        }

        [Test]
        public void LogLevel_DefaultMethod_MethodIsLoggedWithDebugLevel()
        {
            SimpleClass2 s = new SimpleClass2();
            s.NormalMethod();

            string output = OutputString.ToString();
            StringAssert.Contains("DEBUG TestAssembly.SimpleClass2 (null) - Entering: TestAssembly.SimpleClass2.NormalMethod()", output);
            StringAssert.Contains("DEBUG TestAssembly.SimpleClass2 (null) - Leaving: TestAssembly.SimpleClass2.NormalMethod()", output);
        }

        [Test]
        public void LogLevel_ErrorMethod_MethodIsLoggedWithErrorLevel()
        {
            SimpleClass2 s = new SimpleClass2();
            s.ErrorMethod();

            string output = OutputString.ToString();
            StringAssert.Contains("ERROR TestAssembly.SimpleClass2 (null) - Entering: TestAssembly.SimpleClass2.ErrorMethod()", output);
            StringAssert.Contains("ERROR TestAssembly.SimpleClass2 (null) - Leaving: TestAssembly.SimpleClass2.ErrorMethod()", output);
        }
    }
}