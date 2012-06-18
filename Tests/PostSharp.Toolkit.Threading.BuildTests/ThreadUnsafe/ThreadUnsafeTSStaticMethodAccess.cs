namespace PostSharp.Toolkit.Threading.BuildTests.ThreadUnsafe
{
    namespace ThreadUnsafeTSStaticMethodAccess
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

           
        }

        [ThreadUnsafeObject]
        class ThreadUnsafeCklass
        {
            [ThreadSafe]
            private int field;

            private static void StaticThreadUnsafe(ThreadUnsafeCklass instance)
            {
                instance.Method();
                instance.field = 5;
            }

            [ThreadUnsafeMethod]
            private void Method()
            {}
        }
    }
}
