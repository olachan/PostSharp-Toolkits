using System;
using System.Linq;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Custom attribute specifying that marked Method is idempotent exp. that its result depends only upon its parameters.  
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MulticastAttributeUsage(MulticastTargets.Method, PersistMetaData = true)]
    public class IdempotentMethodAttribute : MethodLevelAspect
    {
        public override bool CompileTimeValidate(MethodBase method)
        {
            if (!method.HasOnlyIntrinsicOrObjectParameters())
            {
                DomainMessageSource.Instance.Write( method, SeverityType.Error, "INPC004", method.FullName() );
                return false;
            }

            return base.CompileTimeValidate( method );
        }
    }
}