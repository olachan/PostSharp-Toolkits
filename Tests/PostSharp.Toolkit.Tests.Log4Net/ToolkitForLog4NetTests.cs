using NUnit.Framework;
using TestAssembly;
using log4net.Config;

namespace PostSharp.Toolkit.Tests.Log4Net
{
    [TestFixture]
    public class ToolkitForLog4NetTests : BaseTestsFixture
    {
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            BasicConfigurator.Configure();
        }

        [Test]
        public void Log4Net_Methods_LogsMethodEnter()
        {
            SimpleClass s = new SimpleClass();
            s.Method1();

            string output = OutputString.ToString();
            StringAssert.Contains("DEBUG TestAssembly.SimpleClass (null) - Entering: TestAssembly.SimpleClass.Method1()", output);
        }

        [Test]
        public void Log4Net_Properties_LogsPropertyGetter()
        {
            SimpleClass s = new SimpleClass();
            string value = s.Property1;

            string output = OutputString.ToString();
            StringAssert.Contains("DEBUG TestAssembly.SimpleClass (null) - Entering: TestAssembly.SimpleClass.get_Property1()", output);
        }

        [Test]
        public void Log4Net_Properties_LogsPropertySetter()
        {
            SimpleClass s = new SimpleClass();
            s.Property1 = "Test";

            string output = OutputString.ToString();
            StringAssert.Contains("DEBUG TestAssembly.SimpleClass (null) - Entering: TestAssembly.SimpleClass.set_Property1(string value = \"Test\")", output);
        }

        [Test]
        public void Log4Net_OnException_PrintsException()
        {
            SimpleClass s = new SimpleClass();
            try
            {
                s.MethodThrowsException();
            }
            catch { }

            string output = OutputString.ToString();
            StringAssert.Contains("An exception occurred:\nSystem.Exception", output);
        }

        [Test]
        public void Log4net_UserDefinedType_DoesNotLogMethodCallsRecursively()
        {
            Person person = new Person
            {
                FirstName = "John",
                LastName = "Smith"
            };

            string s = person.ToString();
            string output = OutputString.ToString();
            StringAssert.Contains("Entering: PostSharp.Toolkit.Tests.Log4Net.Person.GetFirstName(PostSharp.Toolkit.Tests.Log4Net.Person person = {John Smith})", output);
        }

        [Test]
        public void LogLevel_ErrorMethod_MethodIsLoggedWithErrorLevel()
        {
            LogLevelTestClass s = new LogLevelTestClass();
            s.ErrorMethod();

            string output = OutputString.ToString();
            StringAssert.Contains(" ERROR TestAssembly.LogLevelTestClass (null) - Entering: TestAssembly.LogLevelTestClass.ErrorMethod()", output);
            StringAssert.Contains(" ERROR TestAssembly.LogLevelTestClass (null) - Leaving: TestAssembly.LogLevelTestClass.ErrorMethod()", output);
        }

        [Test]
        public void ParameterOptions_ThisParameter_ValueOfThisParameterIsPrinted()
        {
            ThisArgumentTestClass s = new ThisArgumentTestClass();
            s.LogThisArgument();

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.ThisArgumentTestClass.LogThisArgument(this = {TestAssembly.ThisArgumentTestClass})", output);
        }
    }
}