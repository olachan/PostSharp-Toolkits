// @ExpectedMessage(THR012)
namespace PostSharp.Toolkit.Threading.BuildTests.ThreadUnsafe
{
    namespace ThreadUnsafeStaticMethodFieldAccess
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
            private int field;

            private static void StaticThreadUnsafe(ThreadUnsafeCklass instance)
            {
                instance.field = 5;
            }
        }
    }
}
