// @ExpectedMessage(THR002)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.ThreadUnsafe
{
    namespace ThreadUnsafeOnClassWithPublicFields
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

           
        }

        [ThreadUnsafeObject]
        class SomeClass
        {
            [ThreadUnsafeMethod]
            private static void StaticThreadUnsafe()
            {

            }
        }
    }
}
