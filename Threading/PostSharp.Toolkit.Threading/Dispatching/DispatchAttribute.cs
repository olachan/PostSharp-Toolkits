using System;
using System.Reflection;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    [AttributeUsage(AttributeTargets.Method)]
    [ProvideAspectRole(StandardRoles.Threading)]
    [Serializable]
    public class DispatchAttribute : MethodInterceptionAspect
    {
        public bool IsAsync { get; set; }

        public DispatchAttribute(bool isAsync = false)
        {
            IsAsync = isAsync;
        }

        public override bool CompileTimeValidate(MethodBase method)
        {
            var methodInfo = method as MethodInfo;
            
            if (methodInfo == null)
            {
                Message.Write(method, SeverityType.Error, "THREADING.DISPATCH01",
                                "Cannot apply DispatchAttribute to a constructor on class {0}.",
                               method.DeclaringType.Name);
                return false;
            }

            
            if (this.IsAsync && methodInfo.ReturnType != typeof(void))
            {
                Message.Write(method, SeverityType.Error, "THREADING.DISPATCH02",
                                "Asynchronous DispatchAttribute cannot be applied to {0}.{1}. It can only be applied to void methods.",
                               method.DeclaringType.Name, method.Name);
                return false;
            }

            if (!(method.DeclaringType is IThreadAffined) &&
                    method.DeclaringType.GetCustomAttributes(typeof(ThreadAffinedAttribute), true).Length == 0)
            {

                Message.Write(method, SeverityType.Error, "THREADING.DISPATCH03",
                               "DispatchAttribute cannot be applied to {0}.{1}. It can only be applied to methods in classes implementing "+
                               "IThreadAffined or marked with ThreadAffinedAttribute.",
                               method.DeclaringType.Name, method.Name);
            }

            return base.CompileTimeValidate(method);
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            var threadAffined = args.Instance as IThreadAffined;
            SynchronizationContext sync = threadAffined.SynchronizationContext;
           
            if (sync == null)
            {
                throw new InvalidOperationException("Cannot dispatch method: synchronization context is null");
            }

            if (this.IsAsync)
            {
                sync.Post((o) => args.Proceed(), null);
            }
            else
            {
                sync.Send((o) => args.Proceed(), null);
            }
        }


       
    }

}