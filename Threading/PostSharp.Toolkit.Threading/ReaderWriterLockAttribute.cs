#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading
{
    [Serializable]
    [MulticastAttributeUsage( MulticastTargets.Method, TargetMemberAttributes = MulticastAttributes.Instance, AllowMultiple = false )]
    [ProvideAspectRole(StandardRoles.Threading)]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, StandardRoles.Tracing)]
    public abstract class ReaderWriterLockAttribute : OnMethodBoundaryAspect
    {
        private bool useDeadlockDetection;

        internal bool UseDeadlockDetection
        {
            get { return this.useDeadlockDetection; }
        }

        public override void CompileTimeInitialize( MethodBase method, AspectInfo aspectInfo )
        {
            Attribute[] attributes = GetCustomAttributes( method.DeclaringType.Assembly, typeof(DeadlockDetectionPolicy) );
            this.useDeadlockDetection = attributes.Length > 0;
        }
    }
}