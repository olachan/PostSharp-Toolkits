using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [Serializable]
    public class ImplicitOperationManagementAttribute : ObjectAccessorsMapSerializingAspect, ITrackedObject
    {
        [NonSerialized]
        protected Dictionary<MemberInfoIdentity, MethodDescriptor> MethodAttributes;

        protected HashSet<string> TrackedFields;

        [NonSerialized]
        private IObjectTracker tracker;

        private string MethodOperationStringFormat
        {
            get
            {
                return this.ThisTracker.OperationNameGenerationConfiguration.MethodOperationStringFormat;
            }
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            this.TrackedFields = new HashSet<string>();

            foreach (var propertyInfo in type.GetProperties(BindingFlagsSet.AllInstanceDeclared).Where(f => f.IsDefined(typeof(ChangeTrackedAttribute), false)))
            {
                var propertyInfoClosure = propertyInfo;

                var fields = ReflectionSearch.GetDeclarationsUsedByMethod(propertyInfo.GetGetMethod(true))
                    .Select(r => r.UsedDeclaration as FieldInfo)
                    .Where(f => f != null)
                    .Where(f => propertyInfoClosure.PropertyType.IsAssignableFrom(f.FieldType))
                    .Where(f => f.DeclaringType.IsAssignableFrom(type))
                    .ToList();

                if (fields.Count() != 1)
                {
                    DomainMessageSource.Instance.Write(propertyInfo, SeverityType.Error, "INPC013", propertyInfo.Name);
                }
                else
                {
                    this.TrackedFields.Add(fields.First().Name);
                }
            }

            foreach (FieldInfo fieldInfo in type
                .GetFields(BindingFlagsSet.AllInstanceDeclared)
                .Where(f => f.IsDefined(typeof(ChangeTrackedAttribute), false)))
            {
                this.TrackedFields.Add(fieldInfo.Name);
            }

            base.CompileTimeInitialize(type, aspectInfo);
        }

        public override object CreateInstance(AdviceArgs adviceArgs)
        {
            ImplicitOperationManagementAttribute aspect = (ImplicitOperationManagementAttribute)base.CreateInstance(adviceArgs);

            aspect.SetTracker(new AggregateTracker(adviceArgs.Instance));
            aspect.MethodAttributes = GetMethodsAttributes(adviceArgs.Instance.GetType());

            return aspect;
        }

        private static Dictionary<MemberInfoIdentity, MethodDescriptor> GetMethodsAttributes(Type type)
        {
            Dictionary<MemberInfoIdentity, MethodDescriptor> methodAttributes = new Dictionary<MemberInfoIdentity, MethodDescriptor>();
            foreach ( MethodInfo method in type.GetMethods( BindingFlagsSet.PublicInstance ) )
            {
                MethodOperationStrategy operationStrategy = MethodOperationStrategy.Auto;

                if ( method.GetCustomAttributes( typeof(NoAutomaticChangeTrackingOperationAttribute), true ).Any() )
                {
                    operationStrategy = MethodOperationStrategy.Never;
                }
                else if ( method.GetCustomAttributes( typeof(ForceChangeTrackingOperationAttribute), true ).Any() )
                {
                    operationStrategy = MethodOperationStrategy.Always;
                }

                string operationName =
                    method.GetCustomAttributes( typeof(OperationNameAttribute), false ).Select( a => ((OperationNameAttribute)a).Name ).FirstOrDefault();

                methodAttributes.Add(new MemberInfoIdentity( method ), new MethodDescriptor(operationStrategy, operationName));
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
            return
                type.GetMethods(BindingFlagsSet.PublicInstanceDeclared).Where(
                    m =>
                    m.IsDefined(typeof(ForceChangeTrackingOperationAttribute), true) ||
                    (!m.Name.StartsWith("get_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_")));
            
            //TODO: Why are property getters ignored? They may make modifications as well...
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

        public bool IsTracked { get; private set; }

        public int OperationCount { get; private set; }

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

            public MemberInfoIdentity( MemberInfo methodBase )
            {
                this.MetadataToken = methodBase.MetadataToken;
                this.Module = methodBase.Module;
            }

            public bool Equals( MemberInfoIdentity other )
            {
                if ( ReferenceEquals( null, other ) )
                {
                    return false;
                }
                if ( ReferenceEquals( this, other ) )
                {
                    return true;
                }
                return this.MetadataToken == other.MetadataToken && this.Module.Equals( other.Module );
            }

            public override bool Equals( object obj )
            {
                return Equals( (MemberInfoIdentity)obj );
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

    public sealed class OperationNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public OperationNameAttribute(string name)
        {
            Name = name;
        }
    }
}