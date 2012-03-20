using System;
using System.Diagnostics;
using NUnit.Framework;
using TestAssembly;

namespace PostSharp.Toolkit.Tests
{
    [TestFixture]
    public class ParameterValuesLoggingTests : ConsoleTestsFixture
    {
        [ThreadStatic]
        private static bool logField;

        [Test]
        public void LoggingToolkit_UserDefinedType_DoesNotLogMethodCallsRecursively()
        {
            logField = true;
            Person person = new Person
            {
                FirstName = "John",
                LastName = "Smith"
            };
            string s = person.ToString();

            string output = OutputString.ToString();
            
            Console.WriteLine("{0}", person);

            StringAssert.DoesNotContain("get_FirstName", output);
            StringAssert.DoesNotContain("get_LastName", output);
        }

        private class Impl
        {
            public static void TraceWriteLineFormat(string f, object[] p)
            {
                Trace.WriteLine(string.Format(f, p));
            }
        }

    }
}