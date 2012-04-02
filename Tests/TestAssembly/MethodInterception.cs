using System;
using System.Text;
using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Toolkit.Diagnostics;

namespace TestAssembly
{
    [TestAspect(AspectPriority = 1, AttributeTargetElements = MulticastTargets.Method)]
    [Log(AspectPriority = 2, OnSuccessOptions = LogOptions.IncludeReturnValue)]
    public class MethodInterception
    {   
        public StringBuilder Method6(string arg1, object args2, int arg3, DateTime arg4, DateTime? arg5, StringBuilder arg6)
        {
            return arg6;
        }

        public void Method1(string arg)
        {
            
        }
    }

    [Serializable]
    public class TestAspect : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {
            Console.WriteLine("Inside {0}", args.Method.Name);

            args.Proceed();
        }
    }
}