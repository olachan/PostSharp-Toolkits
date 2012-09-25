using NUnit.Framework;

namespace PostSharp.Toolkit.Tests
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class AbstractMethodsTests : BaseTestsFixture
    {
        [Test]
        public void LoggingToolkit_Methods_LogsMethodEnter()
        {
            DerivedFromLogAbstract s = new DerivedFromLogAbstract();
            s.AbstractMethiod();

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: PostSharp.Toolkit.Tests.DerivedFromLogAbstract.AbstractMethiod()", output);
        }
    }

    // ReSharper restore InconsistentNaming 

    public abstract class LogWithAbstract
    {
        public abstract void AbstractMethiod();
    }

    public class DerivedFromLogAbstract : LogWithAbstract
    {
        public override void AbstractMethiod()
        {

        }
    }
}