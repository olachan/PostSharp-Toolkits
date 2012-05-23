#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    /// <summary>
    /// Custom attribute that, when applied on a type, ensures that only one thread executes in methods of this type.
    /// When more than one thread accesses methods of this type, the <see cref="ThreadUnsafeException"/> exception is thrown. 
    /// </summary>
    /// <remarks>
    /// <para>By default, static methods, as well as accesses to static fields, are assumed to be thread-safe. Only instance
    /// methods are checked for concurrent execution, and the domain of exclusion is the instance itself.
    ///  </para>
    /// <para>
    /// When the policy is set to <see cref="ThreadUnsafePolicy.Static"/> (using the relevant constructor), both static and instance
    /// methods are checked for concurrent execution, and the domain of exclusion is the type declaring the method. If the declaring
    /// type is a generic type, the exclusion domain is the generic type instance.
    /// </para>
    /// <para>
    /// This aspect shall be applied to the code only if the project is built with debugging symbols <c>DEBUG</c> or <c>DEBUG_THREADING</c>.
    /// </para>
    /// </remarks>
    [Conditional( "DEBUG" ), Conditional( "DEBUG_THREADING" )]
    [MulticastAttributeUsage( MulticastTargets.Class | MulticastTargets.Struct, PersistMetaData = true )]
    [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    public class ThreadUnsafeClassAttribute : TypeLevelAspect
    {
        private readonly ThreadUnsafePolicy policy;
        private static readonly ConcurrentDictionary<object, Thread> locks = new ConcurrentDictionary<object, Thread>( IdentityComparer<object>.Instance );

        public ThreadUnsafeClassAttribute() : this( ThreadUnsafePolicy.Instance )
        {
        }

        public ThreadUnsafePolicy Policy
        {
            get { return this.policy; }
        }

        public ThreadUnsafeClassAttribute( ThreadUnsafePolicy policy )
        {
            this.policy = policy;
        }


        public override bool CompileTimeValidate( Type type )
        {
            // TODO: All fields of an unsafe class should be private.
            return base.CompileTimeValidate( type );
        }


        [OnMethodEntryAdvice,
         MulticastPointcut(
             Attributes = MulticastAttributes.Instance | MulticastAttributes.Public | MulticastAttributes.Internal | MulticastAttributes.InternalOrProtected )]
        public void OnEnterInstanceMethod( MethodExecutionArgs args )
        {
            TryEnterLock( this.policy, args );
        }

        private static void TryEnterLock( ThreadUnsafePolicy policy, MethodExecutionArgs args )
        {
            object syncObject;

            if ( policy == ThreadUnsafePolicy.Instance )
            {
                syncObject = args.Instance;
            }
            else
            {
                syncObject = args.Method.DeclaringType;
            }

            Thread currentThread = Thread.CurrentThread;
            bool lockAcquired = false;
            locks.AddOrUpdate( syncObject, o =>
                                               {
                                                   lockAcquired = true;
                                                   return currentThread;
                                               },
                               ( o, thread ) =>
                                   {
                                       if ( thread != currentThread )
                                           throw new ThreadUnsafeException();
                                       return thread;
                                   } );


            if ( lockAcquired )
            {
                args.MethodExecutionTag = syncObject;
            }
        }

        [OnMethodExitAdvice( Master = "OnEnterInstanceMethod" )]
        public void OnExitInstanceMethod( MethodExecutionArgs args )
        {
            ExitLock( args );
        }

        private static void ExitLock( MethodExecutionArgs args )
        {
            if ( args.MethodExecutionTag != null )
            {
                Thread thread;
                locks.TryRemove( args.MethodExecutionTag, out thread );
            }
        }

        private IEnumerable<MethodBase> SelectStaticMethods( Type type )
        {
            if ( this.policy == ThreadUnsafePolicy.Instance ) return null;

            return type.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ).Where(
                m => ReflectionHelper.IsInternalOrPublic( m, true ) );
        }


        [OnMethodEntryAdvice, MethodPointcut( "SelectStaticMethods" )]
        public void OnEnterStaticMethod( MethodExecutionArgs args )
        {
            TryEnterLock( ThreadUnsafePolicy.Static, args );
        }

        [OnMethodExitAdvice( Master = "OnEnterStaticMethod" )]
        public void OnExitStaticMethod( MethodExecutionArgs args )
        {
            ExitLock( args );
        }
    }
}