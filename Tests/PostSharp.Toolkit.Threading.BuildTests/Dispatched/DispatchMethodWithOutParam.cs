using System;
using System.Collections.Generic;
using System.Text;

namespace PostSharp.Toolkit.Threading.BuildTests.Dispatched
{
    namespace DispatchNonVoidMethod
    {
        class Program
        {
            public static int Main()
            {
                return 1;
            }

            [DispatchedMethod]
            static int Test()
            {
                return 0;
            }
        }
    }
}
