// @ExpectedMessage(INPC003)
// @ExpectedMessage(INPC001)
// @ExpectedMessage(INPC002)
// @ExpectedMessage(PS0060)

// TODO : cope with extra errors when delegate call encountered

using System;

namespace PostSharp.Toolkit.Domain.BuilTests.NPC
{
    namespace NotifyPropertyChangedWithDelegateCall
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }
        }

        [NotifyPropertyChanged]
        class NotifyPropertyChangedWithDelegateCall
        {
            public int Zero
            {
                get
                {
                    return ((Func<int>)(() => 0))();
                }
            }
        }
    }
}
