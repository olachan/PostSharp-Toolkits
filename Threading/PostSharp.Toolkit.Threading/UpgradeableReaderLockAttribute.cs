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
    /// a upgradeable reader lock.
    /// </summary>
    /// <remarks>
    /// <para>The current custom attribute can be applied to instance methods of classes implementing
    /// the <see cref="IReaderWriterSynchronized"/> interface.</para>
    /// </remarks>
    [Serializable]
    [ProvideAspectRole( StandardRoles.Threading )]
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, typeof(ReaderWriterSynchronizedAttribute))]
    public sealed class UpgradeableReaderLockAttribute : ReaderWriterLockAttribute
    {
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
                                                      TypedBinding = EnterReadLockBinding.Instance
                                                  };

                DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnUpgradeableReadEnter( args );
            }
            else
            {
                @lock.EnterUpgradeableReadLock();
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

                DeadlockDetectionPolicy.ReaderWriterEnhancements.Instance.OnUpgradeableReadLockExit( args );
            }

            ((IReaderWriterSynchronized) eventArgs.Instance).Lock.ExitUpgradeableReadLock();
        }

        private sealed class EnterReadLockBinding : MethodBinding
        {
            public static readonly EnterReadLockBinding Instance = new EnterReadLockBinding();

            public override void Invoke( ref object instance, Arguments arguments, object reserved )
            {
                ((ReaderWriterLockSlim) instance).EnterReadLock();
            }
        }
    }
}