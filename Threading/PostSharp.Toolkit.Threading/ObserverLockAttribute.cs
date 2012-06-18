#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Internals;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Custom attribute that, when applied on a method, specifies that it should be executed in
    /// a observer lock. When current thread has writer lock acquired the lock is downgraded to upgradeable reader lock on entry in other case reader lock is acquired. 
    /// On exit state from before invoke is restored.
    /// </summary>
    /// <remarks>
    /// <para>The current custom attribute can be applied to instance methods of classes implementing
    /// the <see cref="IReaderWriterSynchronized"/> interface.</para>
    /// </remarks>
    [Serializable]
    [MulticastAttributeUsage( MulticastTargets.Method | MulticastTargets.Event, TargetMemberAttributes = MulticastAttributes.Instance, AllowMultiple = false )]
    [ProvideAspectRole( StandardRoles.Threading )]
    [AspectTypeDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, typeof(ReaderWriterSynchronizedAttribute) )]
    [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    public class ObserverLockAttribute : Aspect, IEventLevelAspectBuildSemantics, IMethodLevelAspectBuildSemantics
    {
        private static readonly object restoreWriteLockSentinel = new object();

        internal bool UseDeadlockDetection { get; private set; }

        [OnEventInvokeHandlerAdvice]
        [MethodPointcut( "SelectEvents" )]
        public void OnInvoke( EventInterceptionArgs args )
        {
            ReaderWriterLockSlim @lock = ((IReaderWriterSynchronized)args.Instance).Lock;

            bool reEnterWriteLock = @lock.IsWriteLockHeld;

            this.Enter( @lock );

            try
            {
                args.ProceedInvokeHandler();
            }
            finally
            {
                this.Exit( reEnterWriteLock, @lock );
            }
        }

        private IEnumerable<object> SelectEvents( object target )
        {
            if ( target is EventInfo )
            {
                yield return target;
            }
        }

        private IEnumerable<object> SelectMethods( object target )
        {
            MethodBase method = target as MethodBase;
            if ( method != null && !(method.IsSpecialName && (method.Name.StartsWith( "add_" ) || method.Name.StartsWith( "remove_" ))) )
            {
                yield return target;
            }
        }

        /// <summary>
        /// Handler executed before execution of the method to which the current custom attribute is applied.
        /// </summary>
        /// <param name="eventArgs"></param>
        [OnMethodEntryAdvice]
        [MethodPointcut( "SelectMethods" )]
        public void OnEntry( MethodExecutionArgs eventArgs )
        {
            ReaderWriterLockSlim @lock = ((IReaderWriterSynchronized)eventArgs.Instance).Lock;
            if ( @lock.IsWriteLockHeld )
            {
                eventArgs.MethodExecutionTag = restoreWriteLockSentinel;
            }

            this.Enter( @lock );
        }

        private void Enter( ReaderWriterLockSlim @lock )
        {
            if ( @lock.IsWriteLockHeld )
            {
                if ( this.UseDeadlockDetection )
                {
                    MethodExecutionArgs args = new MethodExecutionArgs( @lock, Arguments.Empty );

                    DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnWriterLockExit( args );
                }

                @lock.ExitWriteLock();
            }
            else if ( @lock.IsWriteLockHeld )
            {
                if ( this.UseDeadlockDetection )
                {
                    MethodInterceptionArgs args = new MethodInterceptionArgsImpl( @lock, Arguments.Empty ) { TypedBinding = WriterReadLockBinding.Instance };

                    DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnReaderLockEnter( args );
                }
                else
                {
                    @lock.EnterReadLock();
                }
            }
        }

        /// <summary>
        /// Handler executed after execution of the method to which the current custom attribute is applied.
        /// </summary>
        /// <param name="eventArgs"></param>
        [OnMethodExitAdvice( Master = "OnEntry" )]
        public void OnExit( MethodExecutionArgs eventArgs )
        {
            ReaderWriterLockSlim @lock = ((IReaderWriterSynchronized)eventArgs.Instance).Lock;

            this.Exit( eventArgs.MethodExecutionTag == restoreWriteLockSentinel, @lock );
        }

        private void Exit( bool reEnterWriteLock, ReaderWriterLockSlim @lock )
        {
            if ( reEnterWriteLock )
            {
                if ( this.UseDeadlockDetection )
                {
                    MethodInterceptionArgs args = new MethodInterceptionArgsImpl( @lock, Arguments.Empty ) { TypedBinding = WriterReadLockBinding.Instance };

                    DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnWriterLockEnter( args );
                }
                else
                {
                    @lock.EnterWriteLock();
                }
            }
            else
            {
                if ( this.UseDeadlockDetection )
                {
                    MethodExecutionArgs args = new MethodExecutionArgs( @lock, Arguments.Empty );

                    DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnReaderLockExit( args );
                }

                @lock.ExitReadLock();
            }
        }

        private sealed class WriterReadLockBinding : MethodBinding
        {
            public static readonly WriterReadLockBinding Instance = new WriterReadLockBinding();

            public override void Invoke( ref object instance, Arguments arguments, object reserved )
            {
                ((ReaderWriterLockSlim)instance).EnterWriteLock();
            }
        }

        public void CompileTimeInitialize( EventInfo targetEvent, AspectInfo aspectInfo )
        {
            Attribute[] attributes = GetCustomAttributes( targetEvent.DeclaringType.Assembly, typeof(DeadlockDetectionPolicy) );
            this.UseDeadlockDetection = attributes.Length > 0;
        }

        public void CompileTimeInitialize( MethodBase method, AspectInfo aspectInfo )
        {
            Attribute[] attributes = GetCustomAttributes( method.DeclaringType.Assembly, typeof(DeadlockDetectionPolicy) );
            this.UseDeadlockDetection = attributes.Length > 0;
        }
    }
}