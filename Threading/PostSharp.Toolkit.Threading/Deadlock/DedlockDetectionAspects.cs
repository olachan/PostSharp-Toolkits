// -----------------------------------------------------------------------
// <copyright file="CLRSynchronizationPrymitiveInterceptor.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Toolkit.Threading.Deadlock;

namespace PostSharp.Toolkit.Threading.Deadlock
{
    [Serializable]
    public class DetectDeadlocks : MethodLevelAspect, IAspectProvider
    {
        public IEnumerable<AspectInstance> ProvideAspects(object targetElement)
        {
            MethodBase method = (MethodBase)targetElement;

            if (method.DeclaringType == typeof(Mutex))
            {
                switch (method.Name)
                {
                    case "WaitOne":
                        yield return new AspectInstance(targetElement, new MutexWaitOneDedlockDetection());
                        break;
                    case "ReleaseMutex":
                        yield return new AspectInstance(targetElement, new MutexReleaseMutexDedlockDetection());
                        break;
                }
            }

            if (method.DeclaringType == typeof(Monitor))
            {
                switch (method.Name)
                {
                    case "Enter":
                        yield return new AspectInstance(targetElement, new MonitorEnterDedlockDetection());
                        break;
                    case "TryEnter":
                        yield return new AspectInstance(targetElement, new MonitorTryEnterDedlockDetection());
                        break;
                    case "Exit":
                        yield return new AspectInstance(targetElement, new MonitorExitDedlockDetection());
                        break;
                }
            }

            if (method.DeclaringType == typeof(ReaderWriterLockSlim) || method.DeclaringType == typeof(ReaderWriterLock))
            {
                switch (method.Name)
                {
                    case "EnterReadLock":
                    case "AcquireReaderLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterEnterDedlockDetection(ResourceType.Read));
                        break;
                    case "EnterUpgradeableReadLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterEnterDedlockDetection(ResourceType.UpgradeableRead));
                        break;
                    case "EnterWriteLock":
                    case "AcquireWriterLock":
                    case "UpgradeToWriterLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterEnterDedlockDetection(ResourceType.Write));
                        break;
                    case "ExitReadLock":
                    case "ReleaseReaderLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterExitDedlockDetection(ResourceType.Read));
                        break;
                    case "ExitUpgradeableReadLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterExitDedlockDetection(ResourceType.UpgradeableRead));
                        break;
                    case "ExitWriteLock":
                    case "ReleaseWriterLock":
                    case "DowngradeFromWriterLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterExitDedlockDetection(ResourceType.Write));
                        break;
                    case "TryEnterReadLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterTryEnterDedlockDetection(ResourceType.Read));
                        break;
                    case "TryEnterUpgradeableReadLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterTryEnterDedlockDetection(ResourceType.UpgradeableRead));
                        break;
                    case "TryEnterWriteLock":
                        yield return new AspectInstance(targetElement, new ReaderWriterTryEnterDedlockDetection(ResourceType.Write));
                        break;
                }
            }
        }
    }

    [Serializable]
    public class MutexWaitOneDedlockDetection : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {
            if (args.Arguments.Count == 0 || args.Arguments[0] is bool ||
                (args.Arguments[0] is int && (int)args.Arguments[0] == -1))
            {
                DeadlockMonitor.EnterWaiting(args.Instance, ResourceType.Lock, null);

                try
                {
                    bool result = false;

                    int timeout = 100;

                    Mutex mutex = args.Instance as Mutex;

                    while (!result)
                    {
                        if (timeout > 100)
                        {
                            DeadlockMonitor.DetectDeadlocks();
                        }

                        result = args.Arguments.Count > 0 ? mutex.WaitOne(timeout, (bool)args.Arguments[0]) : mutex.WaitOne(timeout);

                        timeout *= 2;
                    }

                    DeadlockMonitor.ConvertWaitingToAcquired(args.Instance, ResourceType.Lock, null);
                }
                catch (Exception)
                {
                    DeadlockMonitor.ExitWaiting(args.Instance, ResourceType.Lock);
                    throw;
                }
            }
            else
            {
                try
                {
                    DeadlockMonitor.EnterAcquired(args.Instance, ResourceType.Lock, null);
                    args.Proceed();
                }
                catch (Exception)
                {
                    DeadlockMonitor.ExitWaiting(args.Instance, ResourceType.Lock);
                    throw;
                }
            }
        }
    }


    [Serializable]
    public class MutexReleaseMutexDedlockDetection : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            DeadlockMonitor.ExitAcquired(args.Instance, ResourceType.Lock);
        }
    }

    [Serializable]
    public class MonitorEnterDedlockDetection : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {

            DeadlockMonitor.EnterWaiting(args.Arguments[0], ResourceType.Lock, null);

            try
            {
                bool lockTaken = false;

                int timeout = 100;

                while (!lockTaken)
                {
                    if (timeout > 100)
                    {
                        DeadlockMonitor.DetectDeadlocks();
                    }

                    Monitor.TryEnter(args.Arguments[0], timeout, ref lockTaken);

                    if (args.Arguments.Count > 1)
                    {
                        args.Arguments.SetArgument(1, lockTaken);
                    }

                    timeout *= 2;
                }

                DeadlockMonitor.ConvertWaitingToAcquired(args.Arguments[0], ResourceType.Lock, null);
            }
            catch (Exception)
            {
                DeadlockMonitor.ExitWaiting(args.Arguments[0], ResourceType.Lock);
                throw;
            }

        }
    }

    [Serializable]
    public class MonitorTryEnterDedlockDetection : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {

            DeadlockMonitor.EnterWaiting(args.Arguments[0], ResourceType.Lock, null);

            try
            {
                bool lockTaken = false;
                if (args.Arguments[1] is int && (int)args.Arguments[1] == -1)
                {
                    int timeout = 100;

                    while (!lockTaken)
                    {
                        if (timeout > 100)
                        {
                            DeadlockMonitor.DetectDeadlocks();
                        }

                        Monitor.TryEnter(args.Arguments[0], timeout, ref lockTaken);

                        if ((args.Arguments.Count == 2 && args.Arguments[1] is bool) || (args.Arguments.Count == 3 && args.Arguments[2] is bool))
                        {
                            args.Arguments.SetArgument(args.Arguments.Count - 1, lockTaken);
                        }

                        timeout *= 2;
                    }
                }
                else
                {
                    args.Proceed();

                    if ((args.Arguments.Count == 2 && args.Arguments[1] is bool) || (args.Arguments.Count == 3 && args.Arguments[2] is bool))
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
                    DeadlockMonitor.ConvertWaitingToAcquired(args.Arguments[0], ResourceType.Lock, null);
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
    }

    [Serializable]
    public class MonitorExitDedlockDetection : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            DeadlockMonitor.ExitAcquired(args.Arguments[0], ResourceType.Lock);
        }
    }

    [Serializable]
    public class ReaderWriterEnterDedlockDetection : MethodInterceptionAspect
    {
        private readonly ResourceType type;

        public ReaderWriterEnterDedlockDetection(ResourceType type)
        {
            this.type = type;
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            DeadlockMonitor.EnterWaiting(args.Instance, this.type, null);

            try
            {
                bool lockTaken = false;

                int timeout = 100;


                // ReaderWriterLockSlim
                if (args.Arguments.Count == 0)
                {
                    while (!lockTaken)
                    {
                        if (timeout > 100)
                        {
                            DeadlockMonitor.DetectDeadlocks();
                        }

                        var rwl = args.Instance as ReaderWriterLockSlim;
                        switch (this.type)
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

                        timeout *= 2;
                    }
                }
                else
                {
                    if ((int)args.Arguments[0] == Timeout.Infinite)
                    {
                        while (!lockTaken)
                        {
                            if (timeout > 100)
                            {
                                DeadlockMonitor.DetectDeadlocks();
                            }

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
                            
                            timeout *= 2;
                        }

                    }
                    
                    lockTaken = (bool)args.ReturnValue;
                }

                if (lockTaken)
                {
                    DeadlockMonitor.ConvertWaitingToAcquired(args.Instance, this.type, null);
                }
                else
                {
                    DeadlockMonitor.ExitWaiting(args.Instance, this.type);
                }
            }
            catch (Exception)
            {
                DeadlockMonitor.ExitWaiting(args.Instance, this.type);
                throw;
            }
        }
    }

    [Serializable]
    public class ReaderWriterExitDedlockDetection : OnMethodBoundaryAspect
    {
        private ResourceType type;

        public ReaderWriterExitDedlockDetection(ResourceType type)
        {
            this.type = type;
        }

        public override void OnEntry(MethodExecutionArgs args)
        {
            DeadlockMonitor.ExitAcquired(args.Instance, this.type);
        }
    }

    [Serializable]
    public class ReaderWriterTryEnterDedlockDetection : OnMethodBoundaryAspect
    {
        private readonly ResourceType type;

        public ReaderWriterTryEnterDedlockDetection(ResourceType type)
        {
            this.type = type;
        }

        public override void OnSuccess(MethodExecutionArgs args)
        {
            if ((bool)args.ReturnValue)
            {
                DeadlockMonitor.EnterAcquired(args.Arguments[0], this.type, null);
            }
        }
    }

}
