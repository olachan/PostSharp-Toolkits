#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Constraints;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Aspects supporting implementation of actor-base messaging pattern.
    /// See <see cref="Actor"/> for details.
    /// </summary>
    [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    // [AspectRoleDependency(AspectDependencyAction.Conflict, ThreadingToolkitAspectRoles.ThreadingModel)]
    [ProvideAspectRole( ThreadingToolkitAspectRoles.ThreadingModel )]
    [Internal]
    [RequirePostSharp("PostSharp.Toolkit.Threading.Weaver", "PostSharp.Toolkit.Threading", AssemblyReferenceOnly = true)]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, StandardRoles.Tracing)]
    public sealed class ActorAttribute : TypeLevelAspect, IAspectProvider
    {
        public override bool CompileTimeValidate( Type type )
        {
            bool result = base.CompileTimeValidate( type );

            // Check that the attribute is applied on a class derived from Actor (should not be used manually anyway). [ERROR]
            if ( !typeof(Actor).IsAssignableFrom( type ) )
            {
                ThreadingMessageSource.Instance.Write( type, SeverityType.Error, "THR004", type.Name );
                result = false;
            }

            // Check that all fields are private or protected. [ERROR]
            foreach (
                FieldInfo publicField in
                    type.GetFields( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static ).Where(
                        f => f.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length == 0 ) )
            {
                ThreadingMessageSource.Instance.Write( type, SeverityType.Error, "THR005", type.Name, publicField.Name );
                result = false;
            }

            foreach ( MethodInfo method in type.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly )
                .Where( m => m.GetCustomAttributes( typeof(ThreadSafeAttribute), true ).Length == 0 && ReflectionHelper.IsInternalOrPublic( m, false ) ) )
            {
                // Check that the method returns void and does not have ref/out parameters.

                if ( (method.ReturnType != typeof(void) && DispatchedMethodAttribute.GetStateMachineType( method ) == null) ||
                     method.GetParameters().Any( p => p.ParameterType.IsByRef ) )
                {
                    ThreadingMessageSource.Instance.Write( method, SeverityType.Error, "THR009", method.DeclaringType.Name, method.Name );
                    result = false;
                }
            }

            return result;
        }


        // TODO: Check that instance fields, and unprotected instance methods, are only accessed by instance methods, and from the 'this' object. [WARNING]

        // TODO: Cope with callbacks (delegate): make dispatchable if we can, otherwise check that thread is ok (IDispatcher.CheckAccess())

        // TODO: Private interface implementations should be dispatched too.


        IEnumerable<AspectInstance> IAspectProvider.ProvideAspects( object targetElement )
        {
            Type type = (Type) targetElement;

            foreach ( MethodInfo method in type.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ) )
            {
                if ( !ReflectionHelper.IsInternalOrPublic( method, false ) ||
                     method.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length != 0 ) continue;

                yield return new AspectInstance( method, new DispatchedMethodAttribute( true ) );
            }
        }
    }
}