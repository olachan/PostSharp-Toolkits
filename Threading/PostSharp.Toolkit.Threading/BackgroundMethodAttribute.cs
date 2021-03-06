#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Custom attribute that, when applied on a method, makes it asynchronous, i.e.method is started in a new <see cref="Task"/>.
    /// </summary>
    [Serializable]
    [MethodInterceptionAspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    [ProvideAspectRole(StandardRoles.Threading)]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, StandardRoles.Tracing)]
    public sealed class BackgroundMethodAttribute : MethodInterceptionAspect
    {
        // Check that the method returns 'void', has no out/ref argument.
        public override bool CompileTimeValidate( MethodBase method )
        {
            bool result = base.CompileTimeValidate( method );

            MethodInfo methodInfo = (MethodInfo) method;


            if ( methodInfo.ReturnType != typeof(void) || methodInfo.GetParameters().Any( p => p.ParameterType.IsByRef ) )
            {
                ThreadingMessageSource.Instance.Write( method, SeverityType.Error, "THR006", method.DeclaringType.Name, method.Name );

                result = false;
            }


            return result;
        }

        /// <inheritdoc />
        public override void OnInvoke( MethodInterceptionArgs args )
        {
            TaskCreationOptions options = TaskCreationOptions.None;
            if ( this.IsLongRunning ) options |= TaskCreationOptions.LongRunning;

            Task task = new Task( args.Proceed, options );
            task.Start();
        }

        public bool IsLongRunning { get; set; }
    }
}