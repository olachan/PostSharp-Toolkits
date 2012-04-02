using System;
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
            StringAssert.Contains("TestAssembly.StaticClass.GetNumber(System.Int32 number = 42)", output);
            StringAssert.Contains("TestAssembly.StaticClass.GetNumber() : 42", output);
        }

        [Test]
        public void StaticClass_MethodReturningStruct_ValueIsPrinted()
        {
            DateTime dt = new DateTime(1970, 1, 1);
            StaticClass.GetDate(dt);

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.StaticClass.GetDate(System.DateTime dt = {01/01/1970 00:00:00})", output);
            StringAssert.Contains("TestAssembly.StaticClass.GetDate() : {01/01/1970 00:00:00}", output);
        }
    }
}