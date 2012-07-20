using System;
using System.Linq;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Domain
{
    [AttributeUsage(AttributeTargets.Method)]
    [MulticastAttributeUsage(MulticastTargets.Method, PersistMetaData = true)]
    public class IdempotentMethodAttribute : MethodLevelAspect
    {
        public override bool CompileTimeValidate(MethodBase method)
        {
            if (method.GetParameters().Any(p => !p.ParameterType.IsIntrinsic() && !p.ParameterType.IsIntrinsicArray()))
            {
                DomainMessageSource.Instance.Write( method, SeverityType.Error, "INPC004", method.FullName() );
                return false;
            }

            return base.CompileTimeValidate( method );
        }
    }
}