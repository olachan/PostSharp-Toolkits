#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Toolkit.Threading.DeadlockDetection;

namespace PostSharp.Toolkit.Threading
{
    [Serializable]
    public sealed class DeadlockDetectionPolicy : AssemblyLevelAspect, IAspectProvider
    {
        public override bool CompileTimeValidate( _Assembly assembly )
        {
            if ( assembly != PostSharpEnvironment.CurrentProject.GetTargetAssembly( false ) )
            {
                Message.Write( assembly, SeverityType.Error, "THR001", "Aspect DeadlockDetectionPolicy must be added to the current assembly only." );
                return false;
            }
            return true;
        }

        public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
        {
            yield return CreateAspectInstance( typeof(Mutex), typeof(MutexEnhancements) );
            yield return CreateAspectInstance( typeof(WaitHandle), typeof(WaitHandleEnhancements) );
            yield return CreateAspectInstance( typeof(Monitor), typeof(MonitorEnhancements) );
            yield return CreateAspectInstance( typeof(ReaderWriterLockSlim), typeof(ReaderWriterEnhancements) );
            yield return CreateAspectInstance( typeof(ReaderWriterLock), typeof(ReaderWriterEnhancements) );
            yield return CreateAspectInstance( typeof(Thread), typeof(ThreadEnhancements) );
        }

        private static AspectInstance CreateAspectInstance( Type targetType, Type aspectType )
        {
            return new AspectInstance( targetType, new ObjectConstruction( aspectType ), null );
        }

        internal static class LockAspectHelper
        {
            private const int initialTimeout = 200;
            private const int secondTimeout = 1000;

            public static void NoTimeoutAcquire( Action enterWaiting, Func<int, bool> getResult, Action convertWaitingToAcquired, Action exitWaiting )
            {
                enterWaiting();

                try
                {
                    bool result = false;

                    int timeout = initialTimeout;

                    while ( !result )
                    {
                        if ( timeout > initialTimeout )
                        {
                            DeadlockMonitor.DetectDeadlocksInternal( Thread.CurrentThread );
                        }

                        result = getResult( timeout <= secondTimeout ? timeout : Timeout.Infinite );

                        timeout = secondTimeout;
                    }

                    convertWaitingToAcquired();
                }
                catch ( Exception )
                {
                    exitWaiting();
                    throw;
                }
            }
        }

        [Serializable]
        [MulticastAttributeUsage( MulticastTargets.Class, AllowMultiple = false )]
        public sealed class ThreadEnhancements : TypeLevelAspect
        {
            [OnMethodInvokeAdvice, MulticastPointcut( MemberName = "Join" )]
            public void OnJoin( MethodInterceptionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () =>
                        {
                            if ( args.Arguments.Count == 0 ||
                                 (args.Arguments[0] is int && (int) args.Arguments[0] == Timeout.Infinite) )
                            {
                                Thread thread = (Thread) args.Instance;
                                LockAspectHelper.NoTimeoutAcquire(
                                    () => DeadlockMonitor.EnterWaiting( thread, ResourceType.Thread ),
                                    thread.Join,
                                    () => { },
                                    () => DeadlockMonitor.ExitWaiting( thread, ResourceType.Thread ) );
                            }
                        } );
            }
        }

        [Serializable]
        [MulticastAttributeUsage( MulticastTargets.Class, AllowMultiple = false )]
        public sealed class WaitHandleEnhancements : TypeLevelAspect
        {
            [OnMethodInvokeAdvice, MulticastPointcut( MemberName = "WaitOne" )]
            public void OnWaitOne( MethodInterceptionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () =>
                        {
                            if ( !(args.Instance is Mutex) )
                            {
                                return;
                            }

                            if ( args.Arguments.Count == 0 || args.Arguments[0] is bool ||
                                 (args.Arguments[0] is int && (int) args.Arguments[0] == Timeout.Infinite) )
                            {
                                Mutex mutex = args.Instance as Mutex;
                                bool? exitContext = args.Arguments.Count > 0 ? (bool?) args.Arguments[0] : null;
                                LockAspectHelper.NoTimeoutAcquire(
                                    () => DeadlockMonitor.EnterWaiting( mutex, ResourceType.Lock ),
                                    timeout =>
                                    exitContext.HasValue
                                        ? mutex.WaitOne( timeout, exitContext.Value )
                                        : mutex.WaitOne( timeout ),
                                    () => DeadlockMonitor.ConvertWaitingToAcquired( mutex, ResourceType.Lock ),
                                    () => DeadlockMonitor.ExitWaiting( mutex, ResourceType.Lock ) );
                            }
                            else
                            {
                                args.Proceed();

                                if ( (bool) args.ReturnValue )
                                {
                                    DeadlockMonitor.EnterAcquired( args.Instance, ResourceType.Lock );
                                }
                            }
                        } );
            }

            [OnMethodInvokeAdvice, MulticastPointcut( MemberName = "WaitAll" )]
            public void OnWaitAll( MethodInterceptionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () =>
                        {
                            WaitHandle[] waitHandles = (WaitHandle[]) args.Arguments[0];

                            if ( args.Arguments.Count == 1 ||
                                 (args.Arguments[0] is int && (int) args.Arguments[0] == Timeout.Infinite) )
                            {
                                bool? exitContext = args.Arguments.Count > 2 ? (bool?) args.Arguments[2] : null;

                                LockAspectHelper.NoTimeoutAcquire(
                                    () =>
                                        {
                                            foreach ( Mutex mutex in waitHandles.OfType<Mutex>() )
                                            {
                                                DeadlockMonitor.EnterWaiting( mutex, ResourceType.Lock );
                                            }
                                        },
                                    timeout =>
                                    exitContext.HasValue
                                        ? WaitHandle.WaitAll( waitHandles, timeout, exitContext.Value )
                                        : WaitHandle.WaitAll( waitHandles, timeout ),
                                    () =>
                                        {
                                            foreach ( Mutex mutex in waitHandles.OfType<Mutex>() )
                                            {
                                                DeadlockMonitor.ConvertWaitingToAcquired( mutex, ResourceType.Lock );
                                            }
                                        },
                                    () =>
                                        {
                                            foreach ( Mutex mutex in waitHandles.OfType<Mutex>() )
                                            {
                                                DeadlockMonitor.ExitWaiting( mutex, ResourceType.Lock );
                                            }
                                        } );
                            }
                            else
                            {
                                args.Proceed();

                                if ( (bool) args.ReturnValue )
                                {
                                    foreach ( Mutex mutex in waitHandles.OfType<Mutex>() )
                                    {
                                        DeadlockMonitor.EnterAcquired( mutex, ResourceType.Lock );
                                    }
                                }
                            }
                        } );
            }

            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "regex:Handle|SafeWaitHandle" )]
            public void OnHandleModification( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () =>
                        {
                            if ( !(args.Instance is Mutex) )
                            {
                                return;
                            }

                            DeadlockMonitor.IgnoreResource( args.Instance, ResourceType.Lock );
                        } );
            }
        }

        [Serializable]
        [MulticastAttributeUsage( MulticastTargets.Class, AllowMultiple = false )]
        public class MutexEnhancements : TypeLevelAspect
        {
            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "Release" )]
            public void OnRelease( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction( () => DeadlockMonitor.ExitAcquired( args.Instance, ResourceType.Lock ) );
            }
        }

        [Serializable]
        [MulticastAttributeUsage( MulticastTargets.Class, AllowMultiple = false )]
        public class MonitorEnhancements : TypeLevelAspect
        {
            [OnMethodInvokeAdvice, MulticastPointcut( MemberName = "Enter" )]
            public void OnEnter( MethodInterceptionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () => LockAspectHelper.NoTimeoutAcquire(
                        () => DeadlockMonitor.EnterWaiting( args.Arguments[0], ResourceType.Lock ),
                        timeout =>
                            {
                                bool lockTaken = false;
                                Monitor.TryEnter( args.Arguments[0], timeout, ref lockTaken );
                                if ( args.Arguments.Count > 1 )
                                {
                                    args.Arguments.SetArgument( 1, lockTaken );
                                }

                                return lockTaken;
                            },
                        () => DeadlockMonitor.ConvertWaitingToAcquired( args.Arguments[0], ResourceType.Lock ),
                        () => DeadlockMonitor.ExitWaiting( args.Arguments[0], ResourceType.Lock ) ) );
            }

            [OnMethodInvokeAdvice, MulticastPointcut( MemberName = "TryEnter" )]
            public void OnTryEnter( MethodInterceptionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () =>
                        {
                            if ( args.Arguments[0] is int && (int) args.Arguments[0] == -1 )
                            {
                                LockAspectHelper.NoTimeoutAcquire(
                                    () => DeadlockMonitor.EnterWaiting( args.Arguments[0], ResourceType.Lock ),
                                    timeout =>
                                        {
                                            bool lockTaken = false;
                                            Monitor.TryEnter( args.Arguments[0], timeout, ref lockTaken );

                                            if ( (args.Arguments.Count == 2 && args.Arguments[1] is bool) ||
                                                 (args.Arguments.Count == 3 && args.Arguments[2] is bool) )
                                            {
                                                args.Arguments.SetArgument( args.Arguments.Count - 1, lockTaken );
                                            }
                                            return lockTaken;
                                        },
                                    () => DeadlockMonitor.ConvertWaitingToAcquired( args.Arguments[0], ResourceType.Lock ),
                                    () => DeadlockMonitor.ExitWaiting( args.Arguments[0], ResourceType.Lock ) );
                            }
                            else
                            {
                                try
                                {
                                    bool lockTaken;

                                    {
                                        DeadlockMonitor.EnterWaiting( args.Arguments[0], ResourceType.Lock );
                                        args.Proceed();

                                        if ( (args.Arguments.Count == 2 && args.Arguments[1] is bool) ||
                                             (args.Arguments.Count == 3 && args.Arguments[2] is bool) )
                                        {
                                            lockTaken = (bool) args.Arguments.GetArgument( args.Arguments.Count - 1 );
                                        }
                                        else
                                        {
                                            lockTaken = (bool) args.ReturnValue;
                                        }
                                    }

                                    if ( lockTaken )
                                    {
                                        DeadlockMonitor.ConvertWaitingToAcquired( args.Arguments[0], ResourceType.Lock );
                                    }
                                    else
                                    {
                                        DeadlockMonitor.ExitWaiting( args.Arguments[0], ResourceType.Lock );
                                    }
                                }
                                catch ( Exception )
                                {
                                    DeadlockMonitor.ExitWaiting( args.Arguments[0], ResourceType.Lock );
                                    throw;
                                }
                            }
                        } );
            }

            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "Exit" )]
            public void OnExit( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction( () => DeadlockMonitor.ExitAcquired( args.Arguments[0], ResourceType.Lock ) );
            }
        }

        [Serializable]
        [MulticastAttributeUsage( MulticastTargets.Class, AllowMultiple = false )]
        public class ReaderWriterEnhancements : TypeLevelAspect
        {
            internal static readonly ReaderWriterEnhancements Instance = new ReaderWriterEnhancements();

            [OnMethodInvokeAdvice, MulticastPointcut( MemberName = "regex:^EnterReadLock|^AcquireReaderLock" )]
            public void OnReaderLockEnter( MethodInterceptionArgs args )
            {
                DeadlockMonitor.ExecuteAction( () => OnEnter( args, ResourceType.Read ) );
            }

            [OnMethodInvokeAdvice, MulticastPointcut( MemberName = "EnterUpgradeableReadLock" )]
            public void OnUpgradeableReadEnter( MethodInterceptionArgs args )
            {
                DeadlockMonitor.ExecuteAction( () => OnEnter( args, ResourceType.UpgradeableRead ) );
            }

            [OnMethodInvokeAdvice, MulticastPointcut( MemberName = "regex:^EnterWriteLock|^AcquireWriterLock" )]
            public void OnWriterLockEnter( MethodInterceptionArgs args )
            {
                DeadlockMonitor.ExecuteAction( () => OnEnter( args, ResourceType.Write ) );
            }

            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "UpgradeToWriterLock")]
            public void OnUpgradeToWriterLock(MethodInterceptionArgs args)
            {
                DeadlockMonitor.ExecuteAction(() => OnEnter(args, ResourceType.Write, false));
            }

            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "regex:^ExitReadLock|^ReleaseReaderLock" )]
            public void OnReaderLockExit( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction( () => ReaderWriterTypeDeadlockMonitorHelper.ExitAcquired( args.Instance, ResourceType.Read ) );
            }

            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "ExitUpgradeableReadLock" )]
            public void OnUpgradeableReadLockExit( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction( () => ReaderWriterTypeDeadlockMonitorHelper.ExitAcquired( args.Instance, ResourceType.UpgradeableRead ) );
            }

            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "regex:^ExitWriteLock|^ReleaseWriterLock" )]
            public void OnWriterLockExit( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction( () => ReaderWriterTypeDeadlockMonitorHelper.ExitAcquired( args.Instance, ResourceType.Write ) );
            }

            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "TryEnterReadLock" )]
            public void OnTryEnterReadLock( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () =>
                        {
                            if ( (bool) args.ReturnValue )
                            {
                                ReaderWriterTypeDeadlockMonitorHelper.EnterAcquired(
                                    args.Arguments[0], ResourceType.Read, null );
                            }
                        } );
            }

            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "TryEnterUpgradeableReadLock" )]
            public void OnTryEnterUpgradeableReadLock( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () =>
                        {
                            if ( (bool) args.ReturnValue )
                            {
                                ReaderWriterTypeDeadlockMonitorHelper.EnterAcquired(
                                    args.Arguments[0], ResourceType.UpgradeableRead, null );
                            }
                        } );
            }

            [OnMethodEntryAdvice, MulticastPointcut( MemberName = "TryEnterWriteLock" )]
            public void OnTryEnterWriteLock( MethodExecutionArgs args )
            {
                DeadlockMonitor.ExecuteAction(
                    () =>
                        {
                            if ( (bool) args.ReturnValue )
                            {
                                ReaderWriterTypeDeadlockMonitorHelper.EnterAcquired(
                                    args.Arguments[0], ResourceType.Write, null );
                            }
                        } );
            }


            internal static void OnEnter( MethodInterceptionArgs args, ResourceType type , bool addAcquierdEdge = true)
            {
                Func<int, bool> acquireLock;

                if ( args.Arguments.Count == 0 ) // when arguments count == 0 it must be ReaderWriterLockSlim
                {
                    acquireLock = timeout =>
                                      {
                                          bool lockTaken;
                                          ReaderWriterLockSlim rwl = (ReaderWriterLockSlim) args.Instance;
                                          switch ( type )
                                          {
                                              case ResourceType.Read:
                                                  lockTaken = rwl.TryEnterReadLock( timeout );
                                                  break;
                                              case ResourceType.Write:
                                                  lockTaken = rwl.TryEnterWriteLock( timeout );
                                                  break;
                                              case ResourceType.UpgradeableRead:
                                                  lockTaken = rwl.TryEnterUpgradeableReadLock( timeout );
                                                  break;
                                              default:
                                                  throw new ArgumentOutOfRangeException();
                                          }
                                          return lockTaken;
                                      };
                }
                else
                {
                    acquireLock = timeout =>
                                      {
                                          bool lockTaken;
                                          args.Arguments.SetArgument( 0, timeout );

                                          try
                                          {
                                              args.Proceed();
                                              lockTaken = true;
                                          }
                                          catch ( ApplicationException )
                                          {
                                              lockTaken = false;
                                          }
                                          return lockTaken;
                                      };
                }

                LockAspectHelper.NoTimeoutAcquire(
                    () => ReaderWriterTypeDeadlockMonitorHelper.EnterWaiting( args.Instance, type, null ),
                    acquireLock,
                    addAcquierdEdge ? () => ReaderWriterTypeDeadlockMonitorHelper.ConvertWaitingToAcquired( args.Instance, type, null ) : (Action)(() => { }),
                    () => ReaderWriterTypeDeadlockMonitorHelper.ExitWaiting( args.Instance, type ) );
            }
        }

        internal static class ReaderWriterTypeDeadlockMonitorHelper
        {
            public static void EnterWaiting( object syncObject, ResourceType syncObjectRole, object syncObjectInfo )
            {
                DeadlockMonitor.EnterWaiting(syncObject, ResourceType.Write);

                //if ( syncObjectRole == ResourceType.Write )
                //{
                //    DeadlockMonitor.EnterWaiting( syncObject, ResourceType.Read );
                //}
            }

            public static void ExitWaiting( object syncObject, ResourceType syncObjectRole )
            {
                DeadlockMonitor.ExitWaiting(syncObject, ResourceType.Write);

                //if ( syncObjectRole == ResourceType.Write )
                //{
                //    DeadlockMonitor.ExitWaiting( syncObject, ResourceType.Read );
                //}
            }

            public static void ConvertWaitingToAcquired( object syncObject, ResourceType syncObjectRole, object syncObjectInfo )
            {
                DeadlockMonitor.ConvertWaitingToAcquired(syncObject, ResourceType.Write);

                //if ( syncObjectRole == ResourceType.Write )
                //{
                //    DeadlockMonitor.ConvertWaitingToAcquired( syncObject, ResourceType.Read );
                //}
            }

            public static void EnterAcquired( object syncObject, ResourceType syncObjectRole, object syncObjectInfo )
            {
                DeadlockMonitor.EnterAcquired(syncObject, ResourceType.Write);

                //if ( syncObjectRole == ResourceType.Write )
                //{
                //    DeadlockMonitor.EnterAcquired( syncObject, ResourceType.Read );
                //}
            }

            public static void ExitAcquired( object syncObject, ResourceType syncObjectRole )
            {
                DeadlockMonitor.ExitAcquired(syncObject, ResourceType.Write);

                //if ( syncObjectRole == ResourceType.Write )
                //{
                //    DeadlockMonitor.ExitAcquired( syncObject, ResourceType.Read );
                //}
            }
        }
    }
}