using System;

namespace TestAssembly
{
    public class SimpleClass
    {
        public string Field1;

        public string Property1 { get; set; }

        public void Method1() { }

        public void MethodThrowsException()
        {
            
            throw new Exception("This is an exception");
        }

        public void MethodWith1Argument(string stringArg)
        {
        }

        public void MethodWith2Arguments(string stringArg, int intArg)
        {
        }

        public void MethodWith3Arguments(string stringArg, int intArg, double doubleArg)
        {
        }

        public void MethodWith4Arguments(string arg0, string arg1, string arg2, string arg3)
        {
        }
    }
}