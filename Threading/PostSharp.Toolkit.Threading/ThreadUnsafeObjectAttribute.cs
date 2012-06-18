#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
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
    /// By default only public methods are verified. <see cref="ThreadUnsafeMethodAttribute"/> can be used to mark private methods that should be checked as well.
    /// <see cref="ThreadSafeAttribute"/> allows exclusion of public methods (marking them as thread-safe).
    /// </para>
    /// <para>
    /// This aspect shall be applied to the code only if the project is built with debugging symbols <c>DEBUG</c> or <c>DEBUG_THREADING</c>.
    /// </para>
    /// </remarks>
    [Conditional( "DEBUG" ), Conditional( "DEBUG_THREADING" )]
    [MulticastAttributeUsage( MulticastTargets.Class | MulticastTargets.Struct, PersistMetaData = true, Inheritance = MulticastInheritance.Strict,
        AllowMultiple = false )]
    [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    //[AspectRoleDependency(AspectDependencyAction.Conflict, ThreadingToolkitAspectRoles.ThreadingModel)]
    //[AspectTypeDependencyAttribute(AspectDependencyAction.Commute, typeof(ThreadUnsafeObjectAttribute))]
    [ProvideAspectRole( ThreadingToolkitAspectRoles.ThreadingModel )]
    public sealed class ThreadUnsafeObjectAttribute : TypeLevelAspect, IAspectProvider
    {
        private readonly ThreadUnsafePolicy policy;

        private static readonly ConcurrentDictionary<object, ThreadHandle> locks =
            new ConcurrentDictionary<object, ThreadHandle>( IdentityComparer<object>.Instance );

        [ThreadStatic] private static HashSet<object> runningConstructors;
        [ThreadStatic] private static Dictionary<object, int> runningThreadSafeMethods;


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
            bool result = base.CompileTimeValidate( type );

            // [ThreadUnsafeMethod] cannot be used on static methods. [Error]
            IEnumerable<MethodInfo> staticThreadUnsafeMethods =
                type.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic ).Where(
                    m => m.GetCustomAttributes( typeof(ThreadUnsafeMethodAttribute), false ).Length != 0 );

            foreach ( MethodInfo staticThreadUnsafeMethod in staticThreadUnsafeMethods )
            {
                ThreadingMessageSource.Instance.Write( staticThreadUnsafeMethod, SeverityType.Error, "THR002", staticThreadUnsafeMethod.DeclaringType.Name,
                                                       staticThreadUnsafeMethod.Name );

                result = false;
            }

            // [ThreadUnsafeMethod] should not be used if policy is "Static". [Warning]
            if ( this.Policy == ThreadUnsafePolicy.Static )
            {
                IEnumerable<MethodInfo> threadUnsafeMethods = type
                    .GetMethods( BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
                    .Where( m => m.GetCustomAttributes( typeof(ThreadUnsafeMethodAttribute), false ).Length != 0 );
                foreach ( MethodInfo threadUnsafeMethod in threadUnsafeMethods )
                {
                    ThreadingMessageSource.Instance.Write( threadUnsafeMethod, SeverityType.Warning, "THR003", threadUnsafeMethod.DeclaringType.Name,
                                                           threadUnsafeMethod.Name );
                }
            }

            // All instance fields should be private or protected unless marked as [ThreadSafe]. [Error]
            foreach (
                FieldInfo publicField in
                    type.GetFields( BindingFlags.Public | BindingFlags.Instance ).Where(
                        f => f.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length == 0 ) )
            {
                ThreadingMessageSource.Instance.Write( type, SeverityType.Error, "THR008", type.Name, publicField.Name );
                result = false;
            }

            // If policy is "Instance", fields cannot be accessed from a static method unless method or field marked as [ThreadSafe]. [Warning]

            if ( this.policy == ThreadUnsafePolicy.Instance )
            {
                IEnumerable<FieldInfo> nonPublicFields =
                    type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttributes(typeof(ThreadSafeAttribute), false).Length == 0);

                foreach ( FieldInfo nonPublicField in nonPublicFields )
                {
                    foreach ( MethodUsageCodeReference codeReference in ReflectionSearch.GetMethodsUsingDeclaration( nonPublicField ) )
                    {
                        if (codeReference.UsingMethod.IsStatic &&
                            codeReference.UsingMethod.GetCustomAttributes(typeof(ThreadSafeAttribute), false).Length == 0)
                        {
                            ThreadingMessageSource.Instance.Write(codeReference.UsingMethod, SeverityType.Warning, "THR012", nonPublicField.Name, codeReference.UsingMethod.Name);
                        }
                    }
                }

                // If policy is "Instance", static methods cannot access instance methods that are not public or internal or [ThreadUnsafeMethod]. [Warning]

                IEnumerable<MethodInfo> nonPublicMethods = type.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                    .Where( m => !ReflectionHelper.IsInternalOrPublic( m, false ) )
                    .Where( m => m.GetCustomAttributes(typeof(ThreadUnsafeMethodAttribute), false).Length == 0 );

                foreach (MethodInfo nonPublicMethod in nonPublicMethods)
                {
                    foreach (MethodUsageCodeReference codeReference in ReflectionSearch.GetMethodsUsingDeclaration(nonPublicMethod))
                    {
                        if (codeReference.UsingMethod.IsStatic &&
                            codeReference.UsingMethod.GetCustomAttributes(typeof(ThreadSafeAttribute), false).Length == 0)
                        {
                            ThreadingMessageSource.Instance.Write(codeReference.UsingMethod, SeverityType.Warning, "THR013", nonPublicMethod.Name, codeReference.UsingMethod.Name);
                        }
                    }
                }
            }


            // TODO: If policy is "Instance", fields of instance A cannot be accessed from an instance method of instance B (A!=B) unless method or field marked as [ThreadSafe]. [Warning]

            return result;
        }

        [OnMethodEntryAdvice, MethodPointcut( "SelectInstanceMethods" )]
        public void OnEnterInstanceMethod( MethodExecutionArgs args )
        {
            if (IsContextThreadSafe(args.Instance)) return;

            TryEnterLock( this.policy, args );
        }

        private IEnumerable<MethodBase> SelectInstanceMethods( Type type )
        {
            if (policy == ThreadUnsafePolicy.ThreadAffined) return null;

            return
                type.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly )
                    .Where( m => m.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length == 0 )
                    .Where(
                        m => ReflectionHelper.IsInternalOrPublic( m, true ) || m.GetCustomAttributes( typeof(ThreadUnsafeMethodAttribute), false ).Length != 0 );
        }

        private void TryEnterLock( ThreadUnsafePolicy policy, MethodExecutionArgs args )
        {
            object syncObject;

            syncObject = GetSyncObject(policy, args.Instance, args.Method.DeclaringType);
            
            if ( runningThreadSafeMethods != null && runningThreadSafeMethods.ContainsKey( syncObject ) ) return;

            ThreadHandle currentThread = new ThreadHandle( Thread.CurrentThread );

            ThreadHandle actualThread = locks.AddOrUpdate( syncObject, o => currentThread,
                                                           ( o, thread ) =>
                                                               {
                                                                   if ( thread.Thread != currentThread.Thread )
                                                                       throw new ThreadUnsafeException(ThreadUnsafeErrorCode.SimultaneousAccess);

                                                                   // Same thread, but different ThreadHandle: we are in a nested call on the same thread.
                                                                   return thread;
                                                               } );


            if ( actualThread == currentThread )
            {
                args.MethodExecutionTag = syncObject;
            }
        }

        private static object GetSyncObject( ThreadUnsafePolicy policy, object instance, Type type )
        {
            object syncObject;
            if ( policy != ThreadUnsafePolicy.Static && instance != null )
            {
                syncObject = instance;
            }
            else
            {
                syncObject = type;
            }
            return syncObject;
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
            if ( this.policy != ThreadUnsafePolicy.Static ) return null;

            return type.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly )
                .Where( m => m.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length == 0 )
                .Where( m => ReflectionHelper.IsInternalOrPublic( m, true ) || m.GetCustomAttributes( typeof(ThreadUnsafeMethodAttribute), false ).Length != 0 );
        }

        private static bool IsContextThreadSafe(object key)
        {
            if (runningThreadSafeMethods == null) return false;

            return (key != null && runningThreadSafeMethods.ContainsKey(key));
        }

        [OnMethodEntryAdvice, MethodPointcut( "SelectStaticMethods" )]
        public void OnEnterStaticMethod( MethodExecutionArgs args )
        {
            if (IsContextThreadSafe(args.Method.DeclaringType)) return;

            TryEnterLock( ThreadUnsafePolicy.Static, args );
        }

        [OnMethodExitAdvice( Master = "OnEnterStaticMethod" )]
        public void OnExitStaticMethod( MethodExecutionArgs args )
        {
            ExitLock( args );
        }

        public bool CheckFieldAccess { get; set; }

        [OnMethodEntryAdvice, MethodPointcut( "SelectConstructors" )]
        public void OnEnterConstructor( MethodExecutionArgs args )
        {
            HashSet<object> myPendingConstructors = runningConstructors;
            if ( runningConstructors == null ) runningConstructors = myPendingConstructors = new HashSet<object>();
            myPendingConstructors.Add( args.Instance );
        }

        [OnMethodExitAdvice( Master = "OnEnterConstructor" )]
        public void OnExitConstructor( MethodExecutionArgs args )
        {
            runningConstructors.Remove( args.Instance );
        }

        public IEnumerable<MethodBase> SelectConstructors( Type type )
        {
            if ( this.CheckFieldAccess )
            {
                return type.GetConstructors( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                    .Union( new[] {type.TypeInitializer} );
            }
            else
            {
                return null;
            }
        }

        [OnLocationSetValueAdvice, MethodPointcut("SelectFields")]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            //If a thread-unsafe field is accessed in a way which ignores checks (e.g. from static method or other instance), an exception should be thrown

            if (runningConstructors != null && runningConstructors.Contains(args.Instance)) return;

            //Fields marked with ThreadSafe have already been exluded

            object key = this.GetRunningThreadSafeMethodsKey(args.Instance, args.Location.DeclaringType);
            if (IsContextThreadSafe(key)) return;


            object sync = GetSyncObject(this.policy, args.Instance, args.Location.DeclaringType);

            ThreadHandle threadHandle;
            if (!locks.TryGetValue(sync, out threadHandle) || threadHandle.Thread != Thread.CurrentThread)
            {
                throw new LockNotHeldException(
                    "Fields not marked with ThreadSafe attribute can only be accessed from monitored ThreadUnsafe methods or methods marked as ThreadSafe.");
            }

            args.ProceedSetValue();
        }

        public IEnumerable<FieldInfo> SelectFields( Type type )
        {
            if ( this.CheckFieldAccess && policy != ThreadUnsafePolicy.ThreadAffined)
            {
                BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
                flags |= (this.policy == ThreadUnsafePolicy.Static) ? BindingFlags.Instance | BindingFlags.Static : BindingFlags.Instance;
                return
                    type.GetFields( flags ).Where(
                        f => f.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length == 0 );
            }
            else
            {
                return null;
            }
        }


        [OnMethodEntryAdvice, MethodPointcut( "SelectThreadSafeMethods" )]
        public void OnEnterThreadSafeMethod( MethodExecutionArgs args )
        {
            runningThreadSafeMethods = runningThreadSafeMethods ?? new Dictionary<object, int>();
            object key = this.GetRunningThreadSafeMethodsKey( args.Instance, args.Method.DeclaringType );
            if ( !runningThreadSafeMethods.ContainsKey( key ) )
            {
                runningThreadSafeMethods[key] = 1;
            }
            else
            {
                runningThreadSafeMethods[key] = runningThreadSafeMethods[key] + 1;
            }
        }

        [OnMethodExitAdvice( Master = "OnEnterThreadSafeMethod" )]
        public void OnExitThreadSafeMethod( MethodExecutionArgs args )
        {
            object key = this.GetRunningThreadSafeMethodsKey( args.Instance, args.Method.DeclaringType );
            int count = runningThreadSafeMethods[key] - 1;
            if ( count == 0 )
            {
                runningThreadSafeMethods.Remove( key );
            }
            else
            {
                runningThreadSafeMethods[key] = count;
            }
        }

        private object GetRunningThreadSafeMethodsKey( object instance, Type declaringType )
        {
            return GetSyncObject(this.policy, instance, declaringType);
            //return (this.policy == ThreadUnsafePolicy.Static || instance == null) ? declaringType : instance;
        }

        private IEnumerable<MethodBase> SelectThreadSafeMethods( Type type )
        {
            return
                type.GetMethods( BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly )
                    .Where( m => m.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length > 0 );
        }


        IEnumerable<AspectInstance> IAspectProvider.ProvideAspects(object targetElement)
        {
            if (this.policy == ThreadUnsafePolicy.ThreadAffined)
            {
                //Unfortunately, cannot have IInstanceScopedAspect on ThreadUnsafeObject itself

                yield return
                    new AspectInstance(targetElement, new ThreadAffinedAspect(null, this.CheckFieldAccess));
            }
        }

        private sealed class ThreadHandle
        {
            public readonly Thread Thread;

            public ThreadHandle(Thread thread)
            {
                this.Thread = thread;
            }
        }

        #region ThreadAffinedAspect

        [Serializable]
        [MulticastAttributeUsage(MulticastTargets.Class | MulticastTargets.Struct, Inheritance = MulticastInheritance.Strict,
        AllowMultiple = false)]
        [ProvideAspectRole(ThreadingToolkitAspectRoles.ThreadingModel)]
        public sealed class ThreadAffinedAspect : TypeLevelAspect, IInstanceScopedAspect
        {
            [NonSerialized]
            private readonly Thread affinedThread;

            private readonly bool checkFieldAccess;


            internal ThreadAffinedAspect(Thread affinedThread, bool checkFieldAccess)
            {
                this.affinedThread = affinedThread;
                this.checkFieldAccess = checkFieldAccess;
            }

            object IInstanceScopedAspect.CreateInstance(AdviceArgs adviceArgs)
            {
                return new ThreadAffinedAspect(Thread.CurrentThread, this.checkFieldAccess);
            }

            void IInstanceScopedAspect.RuntimeInitializeInstance()
            { }

            [OnMethodEntryAdvice, MethodPointcut("SelectInstanceMethods")]
            public void OnEnterInstanceMethod(MethodExecutionArgs args)
            {
                VerifyThreadAffinity();
            }

            private void VerifyThreadAffinity()
            {
                if (this.affinedThread != Thread.CurrentThread)
                {
                    throw new ThreadUnsafeException(ThreadUnsafeErrorCode.InvalidThread);
                }
            }

            private IEnumerable<MethodBase> SelectInstanceMethods(Type type)
            {
                return
                    type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Where(m => m.GetCustomAttributes(typeof(ThreadSafeAttribute), false).Length == 0)
                        .Where(
                            m => ReflectionHelper.IsInternalOrPublic(m, true) || m.GetCustomAttributes(typeof(ThreadUnsafeMethodAttribute), false).Length != 0);
            }

            [OnLocationSetValueAdvice, MethodPointcut("SelectFields")]
            public void OnFieldSet(LocationInterceptionArgs args)
            {
                //Fields marked with ThreadSafe have already been exluded

                if (!IsContextThreadSafe(args.Instance))
                {
                    VerifyThreadAffinity();
                }

                args.ProceedSetValue();
            }

            public IEnumerable<FieldInfo> SelectFields(Type type)
            {
                if (this.checkFieldAccess)
                {
                    return
                        type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(
                            f => f.GetCustomAttributes(typeof(ThreadSafeAttribute), false).Length == 0);
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion
    }
}