using System;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Toolkit.Threading.DeadlockDetection;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    /// <summary>
    /// For internal use. Use <see cref="SynchronizedAttribute"/> insted.
    /// </summary>
    /// <remarks>
    /// Implements synchronizatian for instance methods.
    /// </remarks>
    [Serializable]
    [AspectTypeDependency(AspectDependencyAction.Commute, typeof(SingleThreadedAttribute.SingleThreadedInstanceAttribute))]
    [AspectTypeDependency(AspectDependencyAction.Commute, typeof(SynchronizedInstanceAttribute))]
    [IntroduceInterface(typeof(ISynchronized), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
    [ProvideAspectRole(StandardRoles.Threading)]
    public class SynchronizedInstanceAttribute : LockInstanceAttributeBase, IInstanceScopedAspect, ISynchronized
    {
        /// <summary>
        /// Initializes a new <see cref="SynchronizedInstanceAttribute"/>.
        /// </summary>
        public SynchronizedInstanceAttribute(bool isInstanceLocked = true)
            : base(isInstanceLocked)
        {
            this.AspectPriority = 2;
        }

        public object CreateInstance(AdviceArgs aspectArgs)
        {
            var instance = new SynchronizedInstanceAttribute(this.instanceLocked)
                { useDeadlockDetection = this.useDeadlockDetection };
            return instance;
        }

        public object Lock
        {
            get
            {
                return this.instanceLock;
            }
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            object l = this.instanceLocked ? ((ISynchronized)args.Instance).Lock : this.attributeLock;

            if (useDeadlockDetection)
            {
                MonitorWrapper.Enter(l);
            }
            else
            {
                Monitor.Enter(l);
            }


            try
            {
                args.Proceed();
            }
            finally
            {
                if (useDeadlockDetection)
                {
                    MonitorWrapper.Exit(l);
                }
                else
                {
                    Monitor.Exit(l);
                }

            }
        }
    }
}