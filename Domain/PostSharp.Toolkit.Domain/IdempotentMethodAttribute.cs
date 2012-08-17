#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Custom attribute specifying that marked Method is idempotent exp. that its result depends only upon its parameters.  
    /// </summary>
    [AttributeUsage( AttributeTargets.Method )]
    [MulticastAttributeUsage( MulticastTargets.Method, PersistMetaData = true )]
    public class IdempotentMethodAttribute : MethodLevelAspect
    {
        public override bool CompileTimeValidate( MethodBase method )
        {
            if ( !method.HasOnlyIntrinsicOrObjectParameters() )
            {
                DomainMessageSource.Instance.Write( method, SeverityType.Error, "INPC004", method.FullName() );
                return false;
            }

            return base.CompileTimeValidate( method );
        }
    }
}