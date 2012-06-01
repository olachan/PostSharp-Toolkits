// @ExpectedMessage(THR002)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.ThreadUnsafe
{
    namespace ThreadUnsafeOnStaticMethod
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
            [ThreadUnsafeMethod]
            private static void StaticThreadUnsafe()
            {

            }
        }
    }
}
