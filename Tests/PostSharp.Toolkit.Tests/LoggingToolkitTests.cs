using System;
using NUnit.Framework;
using TestAssembly;

namespace PostSharp.Toolkit.Tests
{
    [TestFixture]
    public class LoggingToolkitTests : ConsoleTestsFixture
    {
        [Test]
        public void LoggingToolkit_Methods_LogsMethodEnter()
        {
            SimpleClass s = new SimpleClass();
            s.Method1();

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: TestAssembly.SimpleClass.Method1()", output);
        }

        [Test]
        public void LoggingToolkit_Properties_LogsPropertyGetter()
        {
            SimpleClass s = new SimpleClass();
            string value = s.Property1;

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: TestAssembly.SimpleClass.get_Property1()", output);
        }

        [Test]
        public void LoggingToolkit_Properties_LogsPropertySetter()
        {
            SimpleClass s = new SimpleClass();
            s.Property1 = "Test";

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: TestAssembly.SimpleClass.set_Property1(string value = \"Test\")", output);
        }

        [Test]
        public void LoggingToolkit_SimpleClassWithFields_LoggingNotAppliedToField()
        {
            SimpleClass s = new SimpleClass();
            s.Field1 = "Test";

            string output = OutputString.ToString();
            StringAssert.DoesNotContain("Field1", output);
        }

        [Test]
        public void LoggingToolkit_OnException_PrintsException()
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
        public void LoggingToolkit_MethodArguments_LogsMethodArgumentNames()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith2Arguments(stringArg: "TEST", intArg: 12345);

            string output = OutputString.ToString();
            StringAssert.Contains("MethodWith2Arguments(string stringArg = \"TEST\", int32 intArg = 12345)", output);
        }

        [Test]
        public void LoggingToolkit_StringArgumentIsNull_PrintsEmptyString()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith1Argument(null);

            string output = OutputString.ToString();
            StringAssert.Contains("MethodWith1Argument(string stringArg = \"\")", output);
        }

        [Test]
        public void LoggingToolkit_UserDefinedType_DoesNotLogMethodCallsRecursively()
        {
            Person person = new Person
            {
                FirstName = "John",
                LastName = "Smith"
            };
            string s = person.ToString();

            string output = OutputString.ToString();
            StringAssert.Contains("PostSharp.Toolkit.Tests.Person.GetFirstName(PostSharp.Toolkit.Tests.Person person = John Smith)", output);
        }
    }
}