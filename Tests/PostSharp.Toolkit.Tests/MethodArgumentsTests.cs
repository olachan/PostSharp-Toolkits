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
            s.MethodWith1Argument("Test");

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.SimpleClass.MethodWith1Argument(System.String stringArg = \"Test\")", output);
        }

        [Test]
        public void MethodArguments_MethodWith2Arguments_LogsArgument()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith2Arguments("Test", 42);

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.SimpleClass.MethodWith2Arguments(System.String stringArg = \"Test\", System.Int32 intArg = 42)", output);
        }

        [Test]
        public void MethodArguments_MethodWith3Arguments_LogsArgument()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith3Arguments("Test", 42, 128.5);

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.SimpleClass.MethodWith3Arguments(System.String stringArg = \"Test\", System.Int32 intArg = 42, System.Double doubleArg = 128.5)", output);
        }

        [Test]
        public void MethodArguments_MethodWith4Arguments_LogsArgument()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith4Arguments("arg0", "arg1", "arg2", "arg3");

            string output = OutputString.ToString();
            StringAssert.Contains("TestAssembly.SimpleClass.MethodWith4Arguments(System.String arg0 = \"arg0\", " + 
                "System.String arg1 = \"arg1\", System.String arg2 = \"arg2\", System.String arg3 = \"arg3\")", output);
        }
    }
}