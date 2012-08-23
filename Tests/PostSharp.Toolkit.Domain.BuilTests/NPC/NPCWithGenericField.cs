// @ExpectedMessage(INPC014)
// @ExpectedMessage(PS0060)

using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.BuilTests.NPC
{
    namespace NPCWithGenericField
    {
        class Program
        {
            public static int Main()
            {
                return 0;
            }
        }

        [NotifyPropertyChanged]
        public class GenericInpc<T>
        {
            public List<T> GenericList { get; set; }
        }
    }
}
