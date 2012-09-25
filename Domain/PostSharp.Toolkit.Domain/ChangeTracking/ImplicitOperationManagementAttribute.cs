using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Toolkit.Domain.Common;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [Serializable]
    public class ImplicitOperationManagementAttribute : ChangeTrackingAspectBase, ITrackedObject
    {
        protected readonly bool EnableTrackingOnTrackerCreation;

        public ImplicitOperationManagementAttribute(bool enableTrackingOnTrackerCreation = false)
        {
            this.EnableTrackingOnTrackerCreation = enableTrackingOnTrackerCreation;
        }

        [NonSerialized]
        protected Dictionary<MemberInfoIdentity, MethodDescriptor> MethodAttributes;

        protected HashSet<string> TrackedFields;

        [NonSerialized]
        private IObjectTracker tracker;

        private string MethodOperationStringFormat
        {
            get
            {
                return this.ThisTracker.NameGenerationConfiguration.MethodOperationStringFormat;
            }
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            this.TrackedFields = this.GetFieldsWithAttribute(type, typeof(NestedTrackedObjectAttribute), "DOM013");

            base.CompileTimeInitialize(type, aspectInfo);
        }

        public override object CreateInstance(AdviceArgs adviceArgs)
        {
            ImplicitOperationManagementAttribute aspect = (ImplicitOperationManagementAttribute)base.CreateInstance(adviceArgs);

            var newTracker = new AggregateTracker(adviceArgs.Instance, this.EnableTrackingOnTrackerCreation);

            aspect.SetTracker(newTracker);

            aspect.MethodAttributes = GetMethodsAttributes(adviceArgs.Instance.GetType());

            return aspect;
        }

        private static Dictionary<MemberInfoIdentity, MethodDescriptor> GetMethodsAttributes(Type type)
        {
            Dictionary<MemberInfoIdentity, MethodDescriptor> methodAttributes = new Dictionary<MemberInfoIdentity, MethodDescriptor>();
            foreach (MethodInfo method in type.GetMethods(BindingFlagsSet.AllInstanceDeclared).Where(m => !m.IsEventAccessor() && (m.IsPublic || m.IsFinal)))
            {
                MethodOperationStrategy operationStrategy = MethodOperationStrategy.Auto;

                if (method.IsDefinedOnMethodOrProperty(typeof(ChangeTrackingIgnoreOperationAttribute), true) ||
                    method.IsDefinedOnMethodOrProperty(typeof(ChangeTrackingIgnoreField), true))
                {
                    operationStrategy = MethodOperationStrategy.Never;
                }

                else if (method.IsDefined(typeof(ChangeTrackingForceOperationAttribute), true))
                {
                    operationStrategy = MethodOperationStrategy.Always;
                }

                string operationName = method.GetCustomAttributeFromMethodOrProperty<OperationNameAttribute>(false).Select(a => a.Name).FirstOrDefault();

                methodAttributes.Add(new MemberInfoIdentity(method), new MethodDescriptor(operationStrategy, operationName));
            }

            return methodAttributes;
        }

        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectMethods")]
        public void OnMethodInvokeBase(MethodInterceptionArgs args)
        {
            MethodInfo methodInfo = (MethodInfo)args.Method;

            if (methodInfo.IsGenericMethod)
            {
                methodInfo = methodInfo.GetGenericMethodDefinition();
            }

            var methodDescriptor = this.MethodAttributes[new MemberInfoIdentity(methodInfo)];
            IDisposable operationScope = null;
            ITrackedObject stackPeek = StackTrace.StackPeek() as ITrackedObject;


            if (methodDescriptor.MethodOperationStrategy == MethodOperationStrategy.Always ||
                (methodDescriptor.MethodOperationStrategy == MethodOperationStrategy.Auto &&
                (stackPeek == null || !ReferenceEquals(stackPeek.Tracker, this.ThisTracker))))
            {
                operationScope = this.ThisTracker.StartImplicitOperationScope(string.Format(this.MethodOperationStringFormat, methodDescriptor.OperationName ?? args.Method.Name));
            }
            try
            {
                StackTrace.PushOnStack(args.Instance);
                args.Proceed();
            }
            finally
            {
                StackTrace.PopFromStack();
                if (operationScope != null)
                {
                    operationScope.Dispose();
                }
            }
        }

        protected IEnumerable<MethodBase> SelectMethods(Type type)
        {
            return type.GetMethods(BindingFlagsSet.AllInstanceDeclared).Where(m => !m.IsEventAccessor() && (m.IsPublic || m.IsFinal));
        }

        public void Undo()
        {
            this.ThisTracker.Undo();
        }

        public void Redo()
        {
            this.ThisTracker.Redo();
        }

        public void AddRestorePoint(string name)
        {
            this.ThisTracker.AddRestorePoint(name);
        }

        public void UndoToRestorePoint(string name)
        {
            this.ThisTracker.UndoTo(name);
        }

        public IObjectTracker Tracker
        {
            get
            {
                return this.tracker;
            }
        }

        public void SetTracker(IObjectTracker tracker)
        {
            this.tracker = tracker;
        }

        public bool IsAggregateRoot
        {
            get
            {
                return ReferenceEquals(this.ThisTracker.AggregateRoot, this.Instance);
            }
        }

        public bool IsTracked
        {
            get
            {
                return this.ThisTracker.IsTracking;
            }
        }

        internal AggregateTracker ThisTracker
        {
            get
            {
                return (AggregateTracker)((ITrackedObject)this.Instance).Tracker;
            }
        }

        protected enum MethodOperationStrategy
        {
            Always,
            Never,
            Auto,
        }

        [Serializable]
        protected sealed class MethodDescriptor
        {
            public MethodOperationStrategy MethodOperationStrategy { get; private set; }

            public string OperationName { get; private set; }

            public MethodDescriptor(MethodOperationStrategy methodOperationStrategy, string operationName = null)
            {
                this.MethodOperationStrategy = methodOperationStrategy;
                this.OperationName = operationName;
            }
        }

        [Serializable]
        public sealed class MemberInfoIdentity : IEquatable<MemberInfoIdentity>
        {
            private int MetadataToken { get; set; }

            private Module Module { get; set; }

            public MemberInfoIdentity(MemberInfo methodBase)
            {
                this.MetadataToken = methodBase.MetadataToken;
                this.Module = methodBase.Module;
            }

            public bool Equals(MemberInfoIdentity other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }
                if (ReferenceEquals(this, other))
                {
                    return true;
                }
                return this.MetadataToken == other.MetadataToken && this.Module.Equals(other.Module);
            }

            public override bool Equals(object obj)
            {
                return Equals((MemberInfoIdentity)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (this.MetadataToken * 397) ^ this.Module.GetHashCode();
                }
            }
        }
    }
}