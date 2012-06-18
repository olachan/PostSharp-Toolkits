// @ExpectedMessage(THR004)
// @ExpectedMessage(THR005)
// @ExpectedMessage(PS0060)
namespace PostSharp.Toolkit.Threading.BuildTests.Actor
{
    namespace ActorOnNotActorDerrivedClass
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }


        }

        [Actor]
        class ActorClass
        {
            public int publicField;

            private void StaticThreadUnsafe()
            {

            }
        }
    }
}
