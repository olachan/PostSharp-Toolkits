using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain
{
    [Serializable]
    internal sealed class ChildPropertyChangedProcessor
    {
        private enum FieldType
        {
            ValueType,
            ReferenceType
        }

        [NonSerialized]
        private Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor> notifyChildPropertyChangedHandlers;

        private Dictionary<string, FieldType> FieldTypes { get; set; }

        private PropertyToFieldBiDirectionalMap propertyToFieldMapping;

        private ChildPropertyChangedProcessor(ChildPropertyChangedProcessor prototype)
        {
            this.FieldTypes = prototype.FieldTypes;
            this.propertyToFieldMapping = new PropertyToFieldBiDirectionalMap( prototype.propertyToFieldMapping );
        }

        private ChildPropertyChangedProcessor(Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies)
        {
            this.CompileTimeInitialize(type, methodFieldDependencies);
        }

        private void CompileTimeInitialize( Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies )
        {
            var fieldTypes = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToDictionary(f => f.FullName(), f => f.FieldType.IsValueType ? FieldType.ValueType : FieldType.ReferenceType);

            this.FieldTypes = fieldTypes.Union(
                type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToDictionary(
                f => f.FullName(), f => f.PropertyType.IsValueType ? FieldType.ValueType : FieldType.ReferenceType)).ToDictionary(kv => kv.Key, kv => kv.Value);

            this.propertyToFieldMapping = new PropertyToFieldBiDirectionalMap();

            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo propertyInfo in allProperties)
            {
                IList<FieldInfo> fieldList;
                if (methodFieldDependencies.TryGetValue(propertyInfo.GetGetMethod(), out fieldList) &&
                    fieldList.Count == 1 &&
                    propertyInfo.PropertyType == fieldList.First().FieldType)
                {
                    this.propertyToFieldMapping.Add(propertyInfo.Name, new FieldByValueDependency(fieldList.First(), type));
                }
            }
        }

        public void RuntimeInitialize()
        {
            this.propertyToFieldMapping.RuntimeInitialize();
        }

        public void ProcessGet(LocationInterceptionArgs args)
        {
            FieldByValueDependency dependentField;
            if (this.propertyToFieldMapping.TryGetByProperty(args.LocationName, out dependentField))
            {
                object value = dependentField.Field.GetValue(args.Instance);

                if (ReferenceEquals(value, args.Value))
                {
                    dependentField.IsActive = true;
                    UnHookNotifyChildPropertyChangedHandler(args);

                }
                else
                {
                    dependentField.IsActive = false;
                    ReHookNotifyChildPropertyChangedHandler(args);
                }
            }
            else
            {
                ReHookNotifyChildPropertyChangedHandler(args);
            }
        }

        public void ReHookNotifyChildPropertyChangedHandler(LocationInterceptionArgs args)
        {
            NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor;
            
            if (this.notifyChildPropertyChangedHandlers.TryGetValue(args.LocationName, out handlerDescriptor) && handlerDescriptor.Reference.IsAlive)
            {
                if (this.IsValueChanged(args.LocationFullName, args.Value, handlerDescriptor.Reference))
                {
                    this.UnHookNotifyChildPropertyChangedHandler(handlerDescriptor);
                    this.HookNotifyChildPropertyChangedHandler(args);
                }
            }
            else
            {
                this.HookNotifyChildPropertyChangedHandler(args);
            }
        }

        private void UnHookNotifyChildPropertyChangedHandler(LocationInterceptionArgs args)
        {
            NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor;

            if (this.notifyChildPropertyChangedHandlers.TryGetValue(args.LocationName, out handlerDescriptor) && handlerDescriptor.Reference.IsAlive)
            {
                this.UnHookNotifyChildPropertyChangedHandler( handlerDescriptor );
            }
        }

        public bool IsValueChanged(string locationFullName, object currentValue, object newValue)
        {
            FieldType fieldType;
            this.FieldTypes.TryGetValue(locationFullName, out fieldType);

            return fieldType == FieldType.ValueType ? !Equals(currentValue, newValue) : !ReferenceEquals(currentValue, newValue);
        }

        private void GenericNotifyChildPropertyChangedEventHandler(string locationName, object instance, object sender, NotifyChildPropertyChangedEventArgs args)
        {
            INotifyChildPropertyChanged incpc = (INotifyChildPropertyChanged)instance;

            incpc.RaiseChildPropertyChanged(new NotifyChildPropertyChangedEventArgs(locationName, args));
        }

        private void HookNotifyChildPropertyChangedHandler(LocationInterceptionArgs args)
        {
            INotifyChildPropertyChanged currentValue = args.Value as INotifyChildPropertyChanged;
            if (currentValue != null)
            {
                string locationName = args.LocationName;
                object instance = args.Instance;
                NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor =
                    new NotifyChildPropertyChangedEventHandlerDescriptor(currentValue, (s, a) => this.GenericNotifyChildPropertyChangedEventHandler(locationName, instance, s, a));
                this.notifyChildPropertyChangedHandlers.AddOrUpdate(locationName, handlerDescriptor);
                currentValue.ChildPropertyChanged += handlerDescriptor.Handler;
            }
        }

        private void UnHookNotifyChildPropertyChangedHandler(NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor)
        {
            INotifyChildPropertyChanged currentValue = handlerDescriptor.Reference.Target as INotifyChildPropertyChanged;
            if (currentValue != null)
            {
                currentValue.ChildPropertyChanged -= handlerDescriptor.Handler;
            }
        }

        [Serializable]
        private sealed class NotifyChildPropertyChangedEventHandlerDescriptor
        {
            public NotifyChildPropertyChangedEventHandlerDescriptor(object reference, EventHandler<NotifyChildPropertyChangedEventArgs> handler)
            {
                this.Reference = new WeakReference(reference);
                this.Handler = handler;
            }

            public WeakReference Reference { get; private set; }

            public EventHandler<NotifyChildPropertyChangedEventArgs> Handler { get; private set; }
        }

        public static ChildPropertyChangedProcessor CompileTimeCreate(Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies)
        {
            return new ChildPropertyChangedProcessor(type, methodFieldDependencies);
        }

        public static ChildPropertyChangedProcessor CreateFromPrototype(ChildPropertyChangedProcessor prototype)
        {
            return new ChildPropertyChangedProcessor(prototype) {notifyChildPropertyChangedHandlers = new Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor>()};
        }

        public IEnumerable<string> GetEffectedPaths( NotifyChildPropertyChangedEventArgs args )
        {
            int dotIndex = args.Path.IndexOf('.');
            if(dotIndex == -1)
            {
                return Enumerable.Empty<string>();
            }

            string changedField = args.Path.Substring(0, dotIndex);
            string changedPath = args.Path.Substring(dotIndex + 1);
            return this.propertyToFieldMapping.GetByField(changedField)
                .Where(d => d.Value.IsActive)
                .Select(d => string.Format("{0}.{1}", d.Key, changedPath));
        }
    }
}
