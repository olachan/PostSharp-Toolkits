// @ExpectedMessage(THR013)
namespace PostSharp.Toolkit.Threading.BuildTests.ThreadUnsafe
{
    namespace ThreadUnsafeStaticMethodAccess
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
            
            private static void StaticThreadUnsafe(ThreadUnsafeCklass instance)
            {
                instance.Method();
            }

            private void Method()
            {}
        }
    }
}
