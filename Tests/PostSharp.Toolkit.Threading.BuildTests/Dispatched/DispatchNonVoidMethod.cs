// @ExpectedMessage(THR001)


namespace PostSharp.Toolkit.Threading.BuildTests.Dispatched
{
    namespace DispatchMethodWithOutParam
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

            [DispatchedMethod(IsAsync=true)]
            static int Test()
            {
                return 0;
            }
        }
    }
}
