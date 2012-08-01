// @ExpectedMessage(INPC011)
// @ExpectedMessage(PS0060)

namespace PostSharp.Toolkit.Domain.BuilTests.NPC
{
    namespace NPCWithVirtualMethod
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }
        }

        [NotifyPropertyChanged]
        class NPCWithVirtualMethod
        {
            public int Zero
            {
                get
                {
                    return this.Get0();
                }
            }


            public virtual int Get0()
            {
                return 0;
            }
        }
    }
}
