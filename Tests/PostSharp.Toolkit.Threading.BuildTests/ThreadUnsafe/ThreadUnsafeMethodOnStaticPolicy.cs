// @ExpectedMessage(THR008)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.ThreadUnsafe
{
    namespace ThreadUnsafeMethodOnStaticPolicy
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

           
        }

        [ThreadUnsafeObject]
        class ThreadUnsafeClass
        {
            public int publicField;

            private void StaticThreadUnsafe()
            {

            }
        }
    }
}
