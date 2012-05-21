using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading.Deadlock
{
    [Serializable]
    public class DeadlockDetectionPolicy : MethodLevelAspect, IAspectProvider
    {
        private static readonly Dictionary<Type, Type> TypesToInstrument;

        static DeadlockDetectionPolicy()
        {
            TypesToInstrument = new Dictionary<Type, Type>
                {
                    { typeof(Mutex), typeof(MutexEnhancements) },
                    { typeof(WaitHandle), typeof(WaitHandleEnhancements) },
                    { typeof(Monitor), typeof(MonitorEnhancements) },
                    { typeof(ReaderWriterLockSlim), typeof(ReaderWriterEnhancements) },
                    { typeof(ReaderWriterLock), typeof(ReaderWriterEnhancements) },
                    { typeof(Thread), typeof(ThreadEnhancements) },
                };
        }

        public IEnumerable<AspectInstance> ProvideAspects(object targetElement)
        {
            var method = (MethodBase)targetElement;

            if (TypesToInstrument.ContainsKey(method.DeclaringType))
            {
                var aspectType = TypesToInstrument[method.DeclaringType];
                TypesToInstrument.Remove(method.DeclaringType);
                yield return new AspectInstance(method.DeclaringType, Activator.CreateInstance(aspectType) as IAspect);
            }
        }

        internal static class LockAspectHelper
        {
            private const int initialTimeout = 200;
            private const int secondTimeout = 1000;

            public static void HandleAbortException(Action action)
            {
                try
                {
                    action();
                }
                catch (ThreadAbortException e)
                {
                    Thread.ResetAbort();
                    throw new DeadlockException(e.ExceptionState != null ? e.ExceptionState.ToString() : null);
                }
            }

            public static void NoTimeoutAcquire(Action enterWaiting, Func<int, bool> getResult, Action convertWaitingToAcquired, Action exitWaiting)
            {
                enterWaiting();

                try
                {
                    bool result = false;

                    int timeout = initialTimeout;

                    while (!result)
                    {
                        if (timeout > initialTimeout)
                        {
                            DeadlockMonitor.DetectDeadlocks(Thread.CurrentThread);
                        }

                        result = getResult(timeout <= secondTimeout ? timeout : Timeout.Infinite);

                        timeout = secondTimeout;
                    }

                    convertWaitingToAcquired();
                }
                catch (Exception)
                {
                    exitWaiting();
                    throw;
                }

            }

        }

        [Serializable]
        [MulticastAttributeUsage(MulticastTargets.Class, AllowMultiple = false)]
        public class ThreadEnhancements : TypeLevelAspect
        {

            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "Join")]
            public void OnJoin(MethodInterceptionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            if (args.Arguments.Count == 0 ||
                                (args.Arguments[0] is int && (int)args.Arguments[0] == Timeout.Infinite))
                            {
                                var thread = args.Instance as Thread;
                                LockAspectHelper.NoTimeoutAcquire(
                                    () => DeadlockMonitor.EnterWaiting(thread, ResourceType.Thread),
                                    timeout => thread.Join(timeout),
                                    () => { },
                                    () => DeadlockMonitor.ExitWaiting(thread, ResourceType.Thread));
                            }
                        });
            }
        }

        [Serializable]
        [MulticastAttributeUsage(MulticastTargets.Class, AllowMultiple = false)]
        public class WaitHandleEnhancements : TypeLevelAspect
        {

            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "WaitOne")]
            public void OnWaitOne(MethodInterceptionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            if (!(args.Instance is Mutex))
                            {
                                return;
                            }

                            if (args.Arguments.Count == 0 || args.Arguments[0] is bool ||
                                (args.Arguments[0] is int && (int)args.Arguments[0] == Timeout.Infinite))
                            {
                                var mutex = args.Instance as Mutex;
                                bool? exitContext = args.Arguments.Count > 0 ? (bool?)args.Arguments[0] : null;
                                LockAspectHelper.NoTimeoutAcquire(
                                    () => DeadlockMonitor.EnterWaiting(mutex, ResourceType.Lock),
                                    timeout =>
                                    exitContext.HasValue
                                        ? mutex.WaitOne(timeout, exitContext.Value)
                                        : mutex.WaitOne(timeout),
                                    () => DeadlockMonitor.ConvertWaitingToAcquired(mutex, ResourceType.Lock),
                                    () => DeadlockMonitor.ExitWaiting(mutex, ResourceType.Lock));
                            }
                            else
                            {
                                args.Proceed();

                                if ((bool)args.ReturnValue)
                                {
                                    DeadlockMonitor.EnterAcquired(args.Instance, ResourceType.Lock);
                                }
                            }
                        });
            }

            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "WaitAll")]
            public void OnWaitAll(MethodInterceptionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            var waitHandles = args.Arguments[0] as WaitHandle[];

                            if (args.Arguments.Count == 1 ||
                                (args.Arguments[1] is int && (int)args.Arguments[0] == Timeout.Infinite))
                            {
                                bool? exitContext = args.Arguments.Count > 2 ? (bool?)args.Arguments[2] : null;

                                LockAspectHelper.NoTimeoutAcquire(
                                    () =>
                                        {
                                            foreach (var mutex in waitHandles.OfType<Mutex>())
                                            {
                                                DeadlockMonitor.EnterWaiting(mutex, ResourceType.Lock);
                                            }
                                        },
                                    timeout =>
                                    exitContext.HasValue
                                        ? WaitHandle.WaitAll(waitHandles, timeout, exitContext.Value)
                                        : WaitHandle.WaitAll(waitHandles, timeout),
                                    () =>
                                        {
                                            foreach (var mutex in waitHandles.OfType<Mutex>())
                                            {
                                                DeadlockMonitor.ConvertWaitingToAcquired(mutex, ResourceType.Lock);
                                            }
                                        },
                                    () =>
                                        {
                                            foreach (var mutex in waitHandles.OfType<Mutex>())
                                            {
                                                DeadlockMonitor.ExitWaiting(mutex, ResourceType.Lock);
                                            }
                                        });
                            }
                            else
                            {
                                args.Proceed();

                                if ((bool)args.ReturnValue)
                                {
                                    foreach (var mutex in waitHandles.OfType<Mutex>())
                                    {
                                        DeadlockMonitor.EnterAcquired(mutex, ResourceType.Lock);
                                    }
                                }
                            }
                        });
            }

            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "regex:Handle|SafeWaitHandle")]
            public void OnHandleModification(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            if (!(args.Instance is Mutex))
                            {
                                return;
                            }

                            DeadlockMonitor.IgnoreResource(args.Instance, ResourceType.Lock);
                        });
            }
        }

        [Serializable]
        [MulticastAttributeUsage(MulticastTargets.Class, AllowMultiple = false)]
        public class MutexEnhancements : TypeLevelAspect
        {
            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "Release")]
            public void OnRelease(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        { DeadlockMonitor.ExitAcquired(args.Instance, ResourceType.Lock); });
            }
        }

        [Serializable]
        [MulticastAttributeUsage(MulticastTargets.Class, AllowMultiple = false)]
        public class MonitorEnhancements : TypeLevelAspect
        {
            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "Enter")]
            public void OnEnter(MethodInterceptionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            LockAspectHelper.NoTimeoutAcquire(
                                () => DeadlockMonitor.EnterWaiting(args.Arguments[0], ResourceType.Lock),
                                timeout =>
                                    {
                                        bool lockTaken = false;
                                        Monitor.TryEnter(args.Arguments[0], timeout, ref lockTaken);
                                        if (args.Arguments.Count > 1)
                                        {
                                            args.Arguments.SetArgument(1, lockTaken);
                                        }

                                        return lockTaken;
                                    },
                                () => DeadlockMonitor.ConvertWaitingToAcquired(args.Arguments[0], ResourceType.Lock),
                                () => DeadlockMonitor.ExitWaiting(args.Arguments[0], ResourceType.Lock));
                        });
            }

            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "TryEnter")]
            public void OnTryEnter(MethodInterceptionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            if (args.Arguments[1] is int && (int)args.Arguments[1] == -1)
                            {
                                LockAspectHelper.NoTimeoutAcquire(
                                    () => DeadlockMonitor.EnterWaiting(args.Arguments[0], ResourceType.Lock),
                                    timeout =>
                                        {
                                            bool lockTaken = false;
                                            Monitor.TryEnter(args.Arguments[0], timeout, ref lockTaken);

                                            if ((args.Arguments.Count == 2 && args.Arguments[1] is bool) ||
                                                (args.Arguments.Count == 3 && args.Arguments[2] is bool))
                                            {
                                                args.Arguments.SetArgument(args.Arguments.Count - 1, lockTaken);
                                            }
                                            return lockTaken;
                                        },
                                    () => DeadlockMonitor.ConvertWaitingToAcquired(args.Arguments[0], ResourceType.Lock),
                                    () => DeadlockMonitor.ExitWaiting(args.Arguments[0], ResourceType.Lock));
                            }
                            else
                            {
                                try
                                {
                                    bool lockTaken;

                                    {
                                        DeadlockMonitor.EnterWaiting(args.Arguments[0], ResourceType.Lock);
                                        args.Proceed();

                                        if ((args.Arguments.Count == 2 && args.Arguments[1] is bool) ||
                                            (args.Arguments.Count == 3 && args.Arguments[2] is bool))
                                        {
                                            lockTaken = (bool)args.Arguments.GetArgument(args.Arguments.Count - 1);
                                        }
                                        else
                                        {
                                            lockTaken = (bool)args.ReturnValue;
                                        }
                                    }

                                    if (lockTaken)
                                    {
                                        DeadlockMonitor.ConvertWaitingToAcquired(args.Arguments[0], ResourceType.Lock);
                                    }
                                    else
                                    {
                                        DeadlockMonitor.ExitWaiting(args.Arguments[0], ResourceType.Lock);
                                    }

                                }
                                catch (Exception)
                                {
                                    DeadlockMonitor.ExitWaiting(args.Arguments[0], ResourceType.Lock);
                                    throw;
                                }
                            }
                        });
            }

            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "Exit")]
            public void OnExit(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        { DeadlockMonitor.ExitAcquired(args.Arguments[0], ResourceType.Lock); });
            }
        }

        [Serializable]
        [MulticastAttributeUsage(MulticastTargets.Class, AllowMultiple = false)]
        public class ReaderWriterEnhancements : TypeLevelAspect
        {
            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "regex:^EnterReadLock|^AcquireReaderLock")]
            public void OnReaderLockEnter(MethodInterceptionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        { this.OnEnter(args, ResourceType.Read); });
            }

            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "EnterUpgradeableReadLock")]
            public void OnUpgradeableReadEnter(MethodInterceptionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        { this.OnEnter(args, ResourceType.UpgradeableRead); });
            }

            [OnMethodInvokeAdvice, MulticastPointcut(MemberName = "regex:^EnterWriteLock|^AcquireWriterLock|^UpgradeToWriterLock")]
            public void OnWriteLockEnter(MethodInterceptionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        { this.OnEnter(args, ResourceType.Write); });
            }

            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "regex:^ExitReadLock|^ReleaseReaderLock")]
            public void OnReadLockExit(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        { ReaderWriterTypeDeadlockMonitorHelper.ExitAcquired(args.Instance, ResourceType.Read); });
            }

            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "ExitUpgradeableReadLock")]
            public void OnUpgradeableReadLockExit(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            ReaderWriterTypeDeadlockMonitorHelper.ExitAcquired(
                                args.Instance, ResourceType.UpgradeableRead);
                        });
            }

            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "regex:^ExitWriteLock|^ReleaseWriterLock|^DowngradeFromWriterLock")]
            public void OnWriteLockExit(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        { ReaderWriterTypeDeadlockMonitorHelper.ExitAcquired(args.Instance, ResourceType.Write); });
            }

            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "TryEnterReadLock")]
            public void OnTryEnterReadLock(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            if ((bool)args.ReturnValue)
                            {
                                ReaderWriterTypeDeadlockMonitorHelper.EnterAcquired(
                                    args.Arguments[0], ResourceType.Read, null);
                            }
                        });
            }

            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "TryEnterUpgradeableReadLock")]
            public void OnTryEnterUpgradeableReadLock(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            if ((bool)args.ReturnValue)
                            {
                                ReaderWriterTypeDeadlockMonitorHelper.EnterAcquired(
                                    args.Arguments[0], ResourceType.UpgradeableRead, null);
                            }
                        });
            }

            [OnMethodEntryAdvice, MulticastPointcut(MemberName = "TryEnterWriteLock")]
            public void OnTryEnterWriteLock(MethodExecutionArgs args)
            {
                LockAspectHelper.HandleAbortException(
                    () =>
                        {
                            if ((bool)args.ReturnValue)
                            {
                                ReaderWriterTypeDeadlockMonitorHelper.EnterAcquired(
                                    args.Arguments[0], ResourceType.Write, null);
                            }
                        });
            }

            public void OnEnter(MethodInterceptionArgs args, ResourceType type)
            {
                Func<int, bool> acquireLock;

                if (args.Arguments.Count == 0)
                {
                    acquireLock = timeout =>
                        {
                            bool lockTaken = false;
                            var rwl = args.Instance as ReaderWriterLockSlim;
                            switch (type)
                            {
                                case ResourceType.Read:
                                    lockTaken = rwl.TryEnterReadLock(timeout);
                                    break;
                                case ResourceType.Write:
                                    lockTaken = rwl.TryEnterWriteLock(timeout);
                                    break;
                                case ResourceType.UpgradeableRead:
                                    lockTaken = rwl.TryEnterUpgradeableReadLock(timeout);
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
                            bool lockTaken = false;
                            args.Arguments.SetArgument(0, timeout);

                            try
                            {
                                args.Proceed();
                                lockTaken = true;
                            }
                            catch (ApplicationException)
                            {
                                lockTaken = false;
                            }
                            return lockTaken;
                        };
                }

                LockAspectHelper.NoTimeoutAcquire(
                    () => ReaderWriterTypeDeadlockMonitorHelper.EnterWaiting(args.Instance, type, null),
                    acquireLock,
                    () => ReaderWriterTypeDeadlockMonitorHelper.ConvertWaitingToAcquired(args.Instance, type, null),
                    () => ReaderWriterTypeDeadlockMonitorHelper.ExitWaiting(args.Instance, type));
            }
        }

        internal static class ReaderWriterTypeDeadlockMonitorHelper
        {
            public static void EnterWaiting(object syncObject, ResourceType syncObjectRole, object syncObjectInfo)
            {
                DeadlockMonitor.EnterWaiting(syncObject, syncObjectRole);

                if (syncObjectRole == ResourceType.Write)
                {
                    DeadlockMonitor.EnterWaiting(syncObject, ResourceType.Read);
                }
            }

            public static void ExitWaiting(object syncObject, ResourceType syncObjectRole)
            {
                DeadlockMonitor.ExitWaiting(syncObject, syncObjectRole);

                if (syncObjectRole == ResourceType.Write)
                {
                    DeadlockMonitor.ExitWaiting(syncObject, ResourceType.Read);
                }
            }

            public static void ConvertWaitingToAcquired(object syncObject, ResourceType syncObjectRole, object syncObjectInfo)
            {
                DeadlockMonitor.ConvertWaitingToAcquired(syncObject, syncObjectRole);

                if (syncObjectRole == ResourceType.Write)
                {
                    DeadlockMonitor.ConvertWaitingToAcquired(syncObject, ResourceType.Read);
                }
            }

            public static void EnterAcquired(object syncObject, ResourceType syncObjectRole, object syncObjectInfo)
            {
                DeadlockMonitor.EnterAcquired(syncObject, syncObjectRole);

                if (syncObjectRole == ResourceType.Write)
                {
                    DeadlockMonitor.EnterAcquired(syncObject, ResourceType.Read);
                }
            }

            public static void ExitAcquired(object syncObject, ResourceType syncObjectRole)
            {
                DeadlockMonitor.ExitAcquired(syncObject, syncObjectRole);

                if (syncObjectRole == ResourceType.Write)
                {
                    DeadlockMonitor.ExitAcquired(syncObject, ResourceType.Read);
                }
            }
        }
    }
}
