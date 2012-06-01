// @ExpectedMessage(THR006)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.BackgroundMethod
{
    namespace BackgroundMethodOnNotVoidMethod
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

            [BackgroundMethod]
            public int MethodReturningNotVoid()
            {
                return 1;
            }
        }
    }
}
