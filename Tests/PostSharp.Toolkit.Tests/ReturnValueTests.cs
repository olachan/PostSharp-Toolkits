using System.Diagnostics;
using NUnit.Framework;
using TestAssembly;

namespace PostSharp.Toolkit.Tests
{
    [TestFixture]
    public class ReturnValueTests : BaseTestsFixture
    {
        private ReturnValueTestClass underTest;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            underTest = new ReturnValueTestClass();
        }

        [Test]
        public void ReturnValue_StringValue_StringValueIsPrintedInQuotes()
        {
            underTest.ReturnsHelloString();

            string output = OutputString.ToString();
            StringAssert.Contains("ReturnValueTestClass.ReturnsHelloString() : \"Hello\"", output);
        }

        [Test]
        public void ReturnValue_IntValue_IntValueIsShown()
        {
            underTest.ReturnsIntValue42();

            string output = OutputString.ToString();
            StringAssert.Contains("ReturnValueTestClass.ReturnsIntValue42() : 42", output);
        }

        [Test]
        public void ReturnValue_ReferenceType_ReturnsReferenceInstance()
        {
            underTest.ReturnsProduct();

            string output = OutputString.ToString();
            StringAssert.Contains("ReturnValueTestClass.ReturnsProduct() : {TestAssembly.Product}", output);
        }

        [Test]
        public void ReturnValue_ReferenceTypeAsObject_ReturnsReferenceInstance()
        {
            underTest.ReturnsProductAsObject();

            string output = OutputString.ToString();
            StringAssert.Contains("ReturnValueTestClass.ReturnsProductAsObject() : {TestAssembly.Product}", output);
        }

        [Test]
        public void ReturnValue_BoxedValue_PrintsBoxedValueInstance()
        {
            underTest.ReturnsBoxedInt();

            string output = OutputString.ToString();
            StringAssert.Contains("ReturnValueTestClass.ReturnsBoxedInt() : {42}", output);
        }

        [Test]
        public void ReturnValue_ValueTypeWithToString_PrintsToStringValue()
        {
            MyStruct result = underTest.ReturnsStruct();

            string output = OutputString.ToString();
            Assert.AreEqual(result.Value, "MyValue");
            StringAssert.Contains("ReturnValueTestClass.ReturnsStruct() : {MyValue}", output);
        }
    }
}