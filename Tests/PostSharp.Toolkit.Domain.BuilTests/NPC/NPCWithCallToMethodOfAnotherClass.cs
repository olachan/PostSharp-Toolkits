// @ExpectedMessage(INPC002)
// @ExpectedMessage(PS0060)

namespace PostSharp.Toolkit.Domain.BuilTests.NPC
{
    namespace NotifyPropertyChangedWithCallToMethodOfAnotherClass
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }
        }

        [NotifyPropertyChanged]
        class NotifyPropertyChangedWithCallToMethodOfAnotherClass
        {
            private AnotherClass anotherClass = new AnotherClass();

            public int Zero
            {
                get
                {
                    return this.anotherClass.Get0();
                }
            }
        }

        class AnotherClass
        {
            public int Zero = 0;

            public int Get0()
            {
                return this.Zero;
            }
        }
    }
}
