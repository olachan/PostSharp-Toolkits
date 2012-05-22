using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    //TODO: Update documentation to current version of the aspect!
    /// <summary>
    /// Custom attribute that, when applied on a method, ensures that only one thread executes in the method
    /// using a simple <see cref="Monitor"/>. When more than one thread accesses the method <see cref="SingleThreadedException"/> is thrown. 
    /// This attribute works only in DEBUG compilation. 
    /// </summary>
    /// <remarks>
    /// When isInstanceLocked is set to true static methods lock on the type, whereas instance methods lock on the instance. 
    /// Only one thread can execute in any of the instance functions, and only one thread can execute in any of a class's static functions.
    /// When isInstanceLocked is set to false only one thread can execute in specific method but many threads can execute in diferent methods.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class SingleThreadedAttribute : MethodLevelAspect, IAspectProvider
    {
        private readonly SingleThreadPolicy policy;

        public SingleThreadedAttribute(SingleThreadPolicy policy = SingleThreadPolicy.Default)
        {
            this.policy = policy;
        }

        public IEnumerable<AspectInstance> ProvideAspects(object targetElement)
        {
            MethodBase method = (MethodBase) targetElement;

            SingleThreadPolicy p = policy;

            if (!method.IsStatic)
            {
                if (p == SingleThreadPolicy.Default) p = SingleThreadPolicy.NonThreadAffinedInstance;

                if (p == SingleThreadPolicy.ClassLevel)
                {
                    Message.Write(method, SeverityType.Error, "THREADING.SINGLEHTREAD01",
                                  "Cannot apply SingleThreadedAttribute with policy SingleThreadPolicy.Class to non-static method {0}.{1}.",
                                  method.DeclaringType.Name, method.Name);
                    yield break;
                }
                yield return new AspectInstance(targetElement, new SingleThreadedInstanceAttribute(p));
            }
            else //static method
            {
                if (p == SingleThreadPolicy.Default) p = SingleThreadPolicy.ClassLevel;

                if (p != SingleThreadPolicy.ClassLevel &&
                    p != SingleThreadPolicy.MethodLevel)
                {
                    Message.Write(method, SeverityType.Error, "THREADING.SINGLEHTREAD02",
                                  "Could not apply SingleThreadedAttribute on {0}.{1}. Only ClassLevel & MethodLevel policies are allowed on static methods.",
                                  method.DeclaringType.Name, method.Name);
                    yield break;
                }
                yield return new AspectInstance(targetElement, new SingleThreadedStaticAttribute(p));
            }
        }

        #region Inner Classes

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public interface ISingleThreaded
        {
            object Lock { get; }
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        [Serializable]
        [AspectTypeDependency(AspectDependencyAction.Commute, typeof(SingleThreadedInstanceAttribute))]
        [AspectTypeDependency(AspectDependencyAction.Commute, typeof(SynchronizedInstanceAttribute))]
        [IntroduceInterface(typeof(ISingleThreaded), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore)]
        [ProvideAspectRole(StandardRoles.Threading)]
        public class SingleThreadedInstanceAttribute : MethodInterceptionAspect, IInstanceScopedAspect, ISingleThreaded
        {
            [NonSerialized]
            protected object instanceLock;

            [NonSerialized]
            protected object attributeLock;

            private readonly SingleThreadPolicy policy;

            [NonSerialized]
            private readonly int affinedThreadId;

            internal SingleThreadedInstanceAttribute(SingleThreadPolicy policy)
                : this(policy, -1)
            { }

            private SingleThreadedInstanceAttribute(SingleThreadPolicy policy, int affinedThreadId)
            {
                if (policy == SingleThreadPolicy.ClassLevel)
                {
                    throw new ArgumentException(
                        "Policy for SingleThreadedInstanceAttribute cannot be set to SingleThreadPolicy.ClassLevel");
                }

                this.policy = policy;
                this.affinedThreadId = affinedThreadId;
            }

            public object CreateInstance(AdviceArgs aspectArgs)
            {
                var instance = new SingleThreadedInstanceAttribute(this.policy, Thread.CurrentThread.ManagedThreadId);
                return instance;
            }

            public void RuntimeInitializeInstance()
            {
                if (this.policy == SingleThreadPolicy.MethodLevel)
                {
                    this.attributeLock = new object();
                }
                else if (this.policy != SingleThreadPolicy.ThreadAffinedInstance)
                {
                    this.instanceLock = new object();
                }
            }

            object ISingleThreaded.Lock
            {
                get
                {
                    return this.instanceLock;
                }
            }

            public override void OnInvoke(MethodInterceptionArgs args)
            {
                switch (this.policy)
                {
                    case SingleThreadPolicy.MethodLevel:
                        VerifyLockAndProceed(args, this.attributeLock);
                        return;
                    case SingleThreadPolicy.NonThreadAffinedInstance:
                        VerifyLockAndProceed(args, ((ISingleThreaded)args.Instance).Lock);
                        return;
                    case SingleThreadPolicy.ThreadAffinedInstance:
                        VerifyThreadAndProceed(args);
                        return;
                    default:
                        throw new NotSupportedException("Invalid policy value"); //Should never happen
                }
            }

            private void VerifyThreadAndProceed(MethodInterceptionArgs args)
            {
                if (Thread.CurrentThread.ManagedThreadId != this.affinedThreadId)
                {
                    throw new SingleThreadedException("An attempt was made to access a single threaded object with thread affinity from another thread.");
                }
            }

            private void VerifyLockAndProceed(MethodInterceptionArgs args, object @lock)
            {
                if (!Monitor.TryEnter(@lock))
                {
                    throw new SingleThreadedException("An attempt was made to simultaneously access a single-threaded method from multiple threads.");
                }
                try
                {
                    args.Proceed();
                }
                finally
                {
                    Monitor.Exit(@lock);
                }
            }
        }


        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        [Serializable]
        [ProvideAspectRole(StandardRoles.Threading)]
        [Conditional("DEBUG")]
        public class SingleThreadedStaticAttribute : MethodImplementationAspect
        {
            protected static ConcurrentDictionary<Type, object> typeLocks = new ConcurrentDictionary<Type, object>();

            [NonSerialized]
            protected object attributeLock;

            private SingleThreadPolicy policy;


            internal SingleThreadedStaticAttribute(SingleThreadPolicy policy)
            {
                if (policy != SingleThreadPolicy.ClassLevel &&
                    policy != SingleThreadPolicy.MethodLevel)
                {
                    throw new ArgumentException("Only ClassLevel & MethodLevel policies can be applied to static methods");
                }
                this.policy = policy;
            }

            public override void RuntimeInitialize(System.Reflection.MethodBase method)
            {
                if (this.policy == SingleThreadPolicy.MethodLevel)
                {
                    this.attributeLock = new object();
                }
            }

            public override void OnInvoke(MethodInterceptionArgs args)
            {
                object l;

                if (this.policy != SingleThreadPolicy.MethodLevel)
                {
                    l = typeLocks.GetOrAdd(args.Method.DeclaringType, (key) => new object());
                }
                else
                {
                    l = this.attributeLock;
                }

                if (!Monitor.TryEnter(l))
                {
                    throw new SingleThreadedException("An attempt was made to simultaneously access a single-threaded method from multiple threads.");
                }

                try
                {
                    args.Proceed();
                }
                finally
                {
                    Monitor.Exit(l);
                }
            }
        }

        #endregion
    }

    //TODO: Document
    public enum SingleThreadPolicy
    {
        MethodLevel,
        ClassLevel, //for static methods only
        ThreadAffinedInstance, //for instance methods only
        NonThreadAffinedInstance, //for instance methods only
        Default //NonThreadAffinedInstance or ClassLevel depending on whether method is static
    }

}
