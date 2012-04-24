﻿using System;
using NUnit.Framework;
using TestAssembly;

namespace PostSharp.Toolkit.Tests.Trace
{
    [TestFixture]
    public class TraceTests : BaseTestsFixture
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [Test]
        public void Trace_Methods_LogsMethodEnter()
        {
            SimpleClass s = new SimpleClass();
            s.Method1();

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: TestAssembly.SimpleClass.Method1()", output);
        }

        [Test]
        public void Trace_Properties_LogsPropertyGetter()
        {
            SimpleClass s = new SimpleClass();
            string value = s.Property1;

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: TestAssembly.SimpleClass.get_Property1()", output);
        }

        [Test]
        public void Trace_Properties_LogsPropertySetter()
        {
            SimpleClass s = new SimpleClass();
            s.Property1 = "Test";

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: TestAssembly.SimpleClass.set_Property1(string value = \"Test\")", output);
        }

        [Test]
        public void Trace_SimpleClassWithFields_LoggingNotAppliedToField()
        {
            SimpleClass s = new SimpleClass();
            s.Field1 = "Test";

            string output = OutputString.ToString();
            StringAssert.DoesNotContain("Field1", output);
        }

        [Test]
        public void Trace_OnException_PrintsException()
        {
            SimpleClass s = new SimpleClass();
            try
            {
                s.MethodThrowsException();
            }
            catch { }

            string output = OutputString.ToString();
            StringAssert.Contains("System.Exception: This is an exception", output);
        }

        [Test]
        public void Trace_UserDefinedType_DoesNotLogMethodCallsRecursively()
        {
            Person person = new Person
            {
                FirstName = "John",
                LastName = "Smith"
            };

            string s = person.ToString();
            string output = OutputString.ToString();
            StringAssert.Contains("PostSharp.Toolkit.Tests.Trace.Person.GetFirstName(PostSharp.Toolkit.Tests.Trace.Person person = {John Smith})", output);
        }


        [Test]
        public void LogLevel_ErrorMethod_MethodIsLoggedWithErrorLevel()
        {
            LogLevelTestClass s = new LogLevelTestClass();
            s.ErrorMethod();

            string output = OutputString.ToString();
            StringAssert.Contains("Error: 0 : Entering: TestAssembly.LogLevelTestClass.ErrorMethod()", output);
            StringAssert.Contains("Error: 0 : Leaving: TestAssembly.LogLevelTestClass.ErrorMethod()", output);
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
