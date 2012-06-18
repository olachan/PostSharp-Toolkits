#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Internals;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Custom attribute that, when applied on a method, specifies that it should be executed in
    /// a reader lock.
    /// </summary>
    /// <remarks>
    /// <para>The current custom attribute can be applied to instance methods of classes implementing
    /// the <see cref="IReaderWriterSynchronized"/> interface.</para>
    /// </remarks>
    [Serializable]
    [MulticastAttributeUsage( MulticastTargets.Method, TargetMemberAttributes = MulticastAttributes.Instance )]
    [ProvideAspectRole( StandardRoles.Threading )]
    [AspectTypeDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, typeof(ReaderWriterSynchronizedAttribute) )]
    [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    public sealed class WriterLockAttribute : ReaderWriterLockAttribute
    {
        /// <summary>
        /// Handler executed before execution of the method to which the current custom attribute is applied.
        /// </summary>
        /// <param name="eventArgs"></param>
        public override void OnEntry( MethodExecutionArgs eventArgs )
        {
            ReaderWriterLockSlim @lock = ((IReaderWriterSynchronized)eventArgs.Instance).Lock;

            if ( this.UseDeadlockDetection )
            {
                MethodInterceptionArgs args = new MethodInterceptionArgsImpl( @lock, Arguments.Empty ) { TypedBinding = WriterReadLockBinding.Instance };

                if ( !@lock.IsUpgradeableReadLockHeld )
                {
                    eventArgs.MethodExecutionTag = new ExitReaderLockLockCookie();
                    DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnUpgradeableReadEnter( args );
                }
                DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnWriterLockEnter( args );
            }
            else
            {
                if ( !@lock.IsUpgradeableReadLockHeld )
                {
                    eventArgs.MethodExecutionTag = new ExitReaderLockLockCookie();
                    @lock.EnterUpgradeableReadLock();
                }
                @lock.EnterWriteLock();
            }
        }

        /// <summary>
        /// Handler executed after execution of the method to which the current custom attribute is applied.
        /// </summary>
        /// <param name="eventArgs"></param>
        public override void OnExit( MethodExecutionArgs eventArgs )
        {
            ReaderWriterLockSlim @lock = ((IReaderWriterSynchronized)eventArgs.Instance).Lock;

            if ( this.UseDeadlockDetection )
            {
                MethodExecutionArgs args = new MethodExecutionArgs( @lock, Arguments.Empty );

                DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnWriterLockExit( args );
                if ( eventArgs.MethodExecutionTag is ExitReaderLockLockCookie )
                {
                    DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnUpgradeableReadLockExit( args );
                }
            }

            @lock.ExitWriteLock();
            if ( eventArgs.MethodExecutionTag is ExitReaderLockLockCookie )
            {
                @lock.ExitUpgradeableReadLock();
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

        private sealed class ExitReaderLockLockCookie
        {
        }
    }
}