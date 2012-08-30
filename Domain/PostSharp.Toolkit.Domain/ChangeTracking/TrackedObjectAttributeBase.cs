using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [Serializable]
    public abstract class TrackedObjectAttributeBase : InstanceLevelAspect, ITrackedObject
    {
        private ObjectAccessorsMap mapForSerialization;

        protected Dictionary<string, MethodSnapshotStrategy> MethodAttributes;

        protected HashSet<string> TrackedFields;

        [NonSerialized]
        private IObjectTracker tracker;

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            //Grab the dependencies map to serialize if, if no other aspect has done it before
            this.mapForSerialization = ObjectAccessorsMap.GetForSerialization();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            //If dependencies map was serialized within this aspect, copy the data to global map
            if (this.mapForSerialization != null)
            {
                this.mapForSerialization.Restore();
                this.mapForSerialization = null;
            }
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            if (!ObjectAccessorsMap.Map.ContainsKey(type))
            {
                ObjectAccessorsMap.Map.Add(type, new ObjectAccessors(type));
            }

            //TODO: What about overloads?!?
            this.MethodAttributes = new Dictionary<string, MethodSnapshotStrategy>();

            foreach (MethodInfo method in type.GetMethods(BindingFlagsSet.PublicInstanceDeclared))
            {
                if (method.GetCustomAttributes(typeof(NoAutomaticChangeTrackingOperationAttribute), true).Any())
                {
                    this.MethodAttributes.Add(method.Name, MethodSnapshotStrategy.Never);
                }
                else if (method.GetCustomAttributes(typeof(ForceChangeTrackingOperationAttribute), true).Any())
                {
                    this.MethodAttributes.Add(method.Name, MethodSnapshotStrategy.Always);
                }
                else
                {
                    this.MethodAttributes.Add(method.Name, MethodSnapshotStrategy.Auto);
                }
            }

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
            TrackedObjectAttributeBase aspect = (TrackedObjectAttributeBase)base.CreateInstance(adviceArgs);

            aspect.SetTracker(new ObjectTracker(adviceArgs.Instance));

            return aspect;
        }

        public void OnMethodInvokeBase(MethodInterceptionArgs args)
        {
            var methodStrategy = this.MethodAttributes[args.Method.Name];
            IDisposable implicitOpertaion = null;
            ITrackedObject stackPeek = StackTrace.StackPeek() as ITrackedObject;
            if (methodStrategy == MethodSnapshotStrategy.Always ||
                (methodStrategy == MethodSnapshotStrategy.Auto && (stackPeek == null || !ReferenceEquals(stackPeek.Tracker, this.ThisTracker))))

            {
                implicitOpertaion = this.ThisTracker.StartImplicitOperation();
            }
            try
            {
                StackTrace.PushOnStack(args.Instance);
                args.Proceed();
            }
            finally
            {
                StackTrace.PopFromStack();
                if (implicitOpertaion != null)
                {
                    implicitOpertaion.Dispose();
                }
            }
        }

        protected IEnumerable<MethodBase> SelectMethods(Type type)
        {
            return
                type.GetMethods(BindingFlagsSet.PublicInstanceDeclared).Where(
                    m =>
                    m.IsDefined(typeof(ForceChangeTrackingOperationAttribute), true) ||
                    (!m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_")));
            
            //TODO: Why are property getters ignored? They may make modifications as well...

            // .Where( m => !m.IsDefined( typeof(DoNotMakeAutomaticSnapshotAttribute), true ) );
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

        public int OperationCount { get; private set; }

        internal ObjectTracker ThisTracker
        {
            get
            {
                return (ObjectTracker)((ITrackedObject)this.Instance).Tracker;
            }
        }

        protected enum MethodSnapshotStrategy
        {
            Always,
            Never,
            Auto,
        }
    }
}