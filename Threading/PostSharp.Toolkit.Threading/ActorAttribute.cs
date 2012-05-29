#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;
using PostSharp.Reflection;
using System.Linq;

namespace PostSharp.Toolkit.Threading
{
    [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    public sealed class ActorAttribute : TypeLevelAspect
    {
        // TODO: Check that the attribute is applied on a class derived from Actor (should not be used manually anyway). [ERROR]

        // TODO: Check that instance fields are only accessed by instance methods [WARNING]

        // TODO: Check that static methods do not access methods not selected by SelectMethods [WARNING]

        // TODO: Cope with callbacks (delegate): make dispatchable if we can, otherwise check that thread is ok (IDispatcher.CheckAccess())

        public IEnumerable<MethodBase> SelectMethods( Type type )
        {
            foreach ( MethodInfo method in type.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ) )
            {
                if ( method.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length == 0 &&
                     ReflectionHelper.IsInternalOrPublic( method, false ) )
                {
                    // TODO: Check that the method returns void and does not have ref/out parameters.

                    yield return method;
                }

                /*
                ReflectionSearch.GetMethodsUsingDeclaration( method ).Any(
                    reference => (reference.Instructions & MethodUsageInstructions.LoadMethodAddress | MethodUsageInstructions.LoadMethodAddressVirtual) != 0 );
                 */
            }
        }

        [OnMethodInvokeAdvice, MethodPointcut( "SelectMethods" )]
        public void OnMethodInvoke( MethodInterceptionArgs args )
        {
            if ( ((Actor) args.Instance).IsDisposed) throw new ObjectDisposedException( args.Instance.ToString() );
            ((IDispatcherObject) args.Instance).Dispatcher.BeginInvoke( new ActorWorkItem( args, true ) );
        }
    }
}