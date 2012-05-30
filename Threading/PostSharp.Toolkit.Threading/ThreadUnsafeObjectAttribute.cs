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
using PostSharp.Reflection;

namespace PostSharp.Toolkit.Threading
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
    [MulticastAttributeUsage( MulticastTargets.Class | MulticastTargets.Struct, PersistMetaData = true, Inheritance = MulticastInheritance.Strict)]
    [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    public sealed class ThreadUnsafeObjectAttribute : TypeLevelAspect
    {
        private readonly ThreadUnsafePolicy policy;
        private static readonly ConcurrentDictionary<object, ThreadHandle> locks = new ConcurrentDictionary<object, ThreadHandle>( IdentityComparer<object>.Instance );

        public ThreadUnsafeObjectAttribute() : this( ThreadUnsafePolicy.Instance )
        {
        }

        public ThreadUnsafePolicy Policy
        {
            get { return this.policy; }
        }

        public ThreadUnsafeObjectAttribute( ThreadUnsafePolicy policy )
        {
            this.policy = policy;
        }


        public override bool CompileTimeValidate( Type type )
        {

            // TODO: Define an attribute [ThreadUnsafeMethodAttribute] used to mark non-public methods.

            // TODO: [ThreadUnsafeMethod] cannot be used on static methods. [Error]

            // TODO: [ThreadUnsafeMethod] should not be used if policy is "Static". [Warning]

            // TODO [NOW]: All instance fields should be private or protected unless marked as [ThreadSafe]. [Error]

            // TODO: If policy is "Instance", fields cannot be accessed from a static method unless method or field marked as [ThreadSafe]. [Warning]

            // TODO: If policy is "Instance", static methods cannot access instance methods that are not public or internal or [ThreadUnsafeMethod]. [Warning]

            // TODO: If policy is "Instance", fields of instance A cannot be accessed from an instance method of instance B (A!=B) unless method or field marked as [ThreadSafe]. [Warning]

            // TODO: [NOW] Dynamic field-access check (exclude constructors from this check, as in ReaderWriterSynchronized).

            return base.CompileTimeValidate( type );
        }

        // TODO [NOW]: Replace MulticastPointcut to SelectMethod, take [ThreadSafe] (exclude inconditionally) and [ThreadUnsafeMethod] (include) into account.

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

            ThreadHandle currentThread = new ThreadHandle(Thread.CurrentThread);

            ThreadHandle actualThread = locks.AddOrUpdate(syncObject, o => currentThread,
                               ( o, thread ) =>
                                   {
                                       if ( thread.Thread != currentThread.Thread )
                                           throw new ThreadUnsafeException();

                                       // Same thread, but different ThreadHandle: we are in a nested call on the same thread.
                                       return thread;
                                   } );


            if (actualThread == currentThread)
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
                ThreadHandle thread;
                locks.TryRemove( args.MethodExecutionTag, out thread );
            }
        }

        private IEnumerable<MethodBase> SelectStaticMethods( Type type )
        {
            if ( this.policy == ThreadUnsafePolicy.Instance ) return null;

            // TODO [NOW]: Check for absence of [ThreadSafe].

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

        sealed class ThreadHandle
        {
            public readonly Thread Thread;

            public ThreadHandle(Thread thread)
            {
                this.Thread = thread;
            }
        }

    }
}