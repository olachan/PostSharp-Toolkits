// @ExpectedMessage(DOM001)
// @ExpectedMessage(PS0060)

namespace PostSharp.Toolkit.Domain.BuilTests.NPC
{
    namespace NotifyPropertyChangedWithDirectAccessToFieldOfAnotherClass
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }
        }

        [NotifyPropertyChanged]
        class NotifyPropertyChangedWithDirectAccessToFieldOfAnotherClass
        {
            private AnotherClass anotherClass = new AnotherClass();

            public int Zero
            {
                get
                {
                    return this.Get0();
                }
            }

            public int Get0()
            {
                return this.anotherClass.Zero;
            }
        }

        class AnotherClass
        {
            public int Zero = 0;
        }
    }
}
