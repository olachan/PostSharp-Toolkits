namespace PostSharp.Toolkit.Threading.BuildTests.ReaderWriterSynchronized
{
    namespace ReaderWriterSynchronizedOnClassWithStaticThreadSafeAccessToField
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
            [ThreadSafe]
            private int field;

            public static void AccessField(ReaderWriterSynchronizedClass instance)
            {
                instance.field = 100;
            }
        }

        [ReaderWriterSynchronized]
        class ReaderWriterSynchronizedClass2
        {
            private int field;

            [ThreadSafe]
            public static void AccessField(ReaderWriterSynchronizedClass2 instance)
            {
                instance.field = 100;
            }
        }
    }
}
