#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Internals;
using PostSharp.Extensibility;
using PostSharp.Toolkit.Threading.DeadlockDetection;

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
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, typeof(ReaderWriterSynchronizedAttribute))]
    public sealed class WriterLockAttribute : ReaderWriterLockAttribute
    {
        // TODO [NOW]: Should be implemented as an upgraded upgradeable reader lock to allow for ObserverLock.

        /// <summary>
        /// Handler executed before execution of the method to which the current custom attribute is applied.
        /// </summary>
        /// <param name="eventArgs"></param>
        public override void OnEntry( MethodExecutionArgs eventArgs )
        {
            ReaderWriterLockSlim @lock = ((IReaderWriterSynchronized) eventArgs.Instance).Lock;

            if ( this.UseDeadlockDetection )
            {
                MethodInterceptionArgs args = new MethodInterceptionArgsImpl( @lock, Arguments.Empty )
                                                  {
                                                      TypedBinding = WriterReadLockBinding.Instance
                                                  };

                DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnUpgradeableReadEnter( args );
                DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnWriterLockEnter( args );
            }
            else
            {
                @lock.EnterUpgradeableReadLock();
                @lock.EnterWriteLock();
            }
        }

        /// <summary>
        /// Handler executed after execution of the method to which the current custom attribute is applied.
        /// </summary>
        /// <param name="eventArgs"></param>
        public override void OnExit( MethodExecutionArgs eventArgs )
        {
            ReaderWriterLockSlim @lock = ((IReaderWriterSynchronized) eventArgs.Instance).Lock;

            if ( this.UseDeadlockDetection )
            {
                MethodExecutionArgs args = new MethodExecutionArgs( @lock, Arguments.Empty );

                DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnWriterLockExit( args );
                DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnUpgradeableReadLockExit( args );
            }

            @lock.ExitWriteLock();
            @lock.ExitUpgradeableReadLock();
        }

        private sealed class WriterReadLockBinding : MethodBinding
        {
            public static readonly WriterReadLockBinding Instance = new WriterReadLockBinding();

            public override void Invoke( ref object instance, Arguments arguments, object reserved )
            {
                
                ((ReaderWriterLockSlim) instance).EnterWriteLock();
            }
        }
    }

    // TODO [NOW]: UpgradeableReaderLock and ObserverLock
}