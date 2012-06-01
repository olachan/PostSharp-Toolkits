// @ExpectedMessage(THR006)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.BackgroundMethod
{
    namespace BackgroundMethodOnRefMethod
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

            [BackgroundMethod]
            public void MethodReturningNotVoid(ref int i)
            {
                return;
            }
        }
    }
}
