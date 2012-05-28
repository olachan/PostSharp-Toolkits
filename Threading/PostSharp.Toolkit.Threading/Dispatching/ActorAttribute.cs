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

namespace PostSharp.Toolkit.Threading.Dispatching
{
    [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    public sealed class ActorAttribute : TypeLevelAspect
    {
        // TODO: Check that the attribute is applied on a class derived from Actor (should not be used manually anyway). [ERROR]

        // TODO: Check that instance fields are only accessed by instance methods [WARNING]

        // TODO: Check that static methods do not access methods not selected by SelectMethods [WARNING]

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
            }
        }

        [OnMethodInvokeAdvice, MethodPointcut( "SelectMethods" )]
        public void OnMethodInvoke( MethodInterceptionArgs args )
        {
            ((IDispatcherObject) args.Instance).Dispatcher.BeginInvoke( new WorkItem( args, true ) );
        }
    }
}