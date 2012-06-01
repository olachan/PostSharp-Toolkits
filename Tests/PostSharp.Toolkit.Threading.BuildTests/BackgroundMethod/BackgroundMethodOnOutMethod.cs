// @ExpectedMessage(THR006)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.BackgroundMethod
{
    namespace BackgroundMethodOnOutMethod
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

            [BackgroundMethod]
            public void MethodReturningNotVoid(out int i)
            {
                i = 1;
                return;
            }
        }
    }
}
