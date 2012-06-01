// @ExpectedMessage(THR007)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.ReaderWriterSynchronized
{
    namespace ReaderWriterSynchronizedOnClassWithPublicFields
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

           
        }

        [ReaderWriterSynchronized]
        class ReaderWriterSynchronizedClass
        {
            public int publicField;

            private void StaticThreadUnsafe()
            {

            }
        }
    }
}
