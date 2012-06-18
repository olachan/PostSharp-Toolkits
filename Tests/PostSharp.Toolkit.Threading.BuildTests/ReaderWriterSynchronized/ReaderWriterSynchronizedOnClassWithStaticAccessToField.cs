// @ExpectedMessage(THR012)
namespace PostSharp.Toolkit.Threading.BuildTests.ReaderWriterSynchronized
{
    namespace ReaderWriterSynchronizedOnClassWithStaticAccessToField
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
            private int field;

            public static void AccessField(ReaderWriterSynchronizedClass instance)
            {
                instance.field = 100;
            }
        }
    }
}
