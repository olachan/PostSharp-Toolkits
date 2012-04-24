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
            int number = StaticClass.GetNumber(42);

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.StaticClass.GetNumber(int number = 42)", output);
            StringAssert.Contains("TestAssembly.StaticClass.GetNumber() : 42", output);
        }

        [Test]
        public void StaticClass_MethodReturningStruct_ValueIsPrinted()
        {
            DateTime dt = new DateTime(1970, 1, 1);
            StaticClass.GetDate(dt);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture= CultureInfo.InvariantCulture;

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.StaticClass.GetDate(DateTime dt = {1970-01-01 00:00:00})", output);
            StringAssert.Contains("TestAssembly.StaticClass.GetDate() : {1970-01-01 00:00:00}", output);
        }
    }
}