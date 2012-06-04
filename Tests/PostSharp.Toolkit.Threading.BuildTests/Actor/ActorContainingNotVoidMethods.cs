// @ExpectedMessage(THR009)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.Actor
{
    namespace ActorContainingNotVoidMethods
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }

           
        }

        class ActorClass : PostSharp.Toolkit.Threading.Actor
        {
            public int StaticThreadUnsafe()
            {
                return 1;
            }
        }
    }
}
