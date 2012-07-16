using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PostSharp.Aspects;

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

        private ChildPropertyChangedProcessor(Dictionary<string, FieldType> fieldIsValueType)
        {
            this.FieldTypes = fieldIsValueType;
        }

        private ChildPropertyChangedProcessor( Type type )
        {
            this.CompileTimeInitialize( type );
        }

        private void CompileTimeInitialize( Type type )
        {
            var fieldTypes = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToDictionary(f => f.FullName(), f => f.FieldType.IsValueType ? FieldType.ValueType : FieldType.ReferenceType);

            this.FieldTypes = fieldTypes.Union(
                type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToDictionary(
                f => f.FullName(), f => f.PropertyType.IsValueType ? FieldType.ValueType : FieldType.ReferenceType)).ToDictionary(kv => kv.Key, kv => kv.Value);
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

        public static ChildPropertyChangedProcessor CompileTimeCreate(Type type)
        {
            return new ChildPropertyChangedProcessor(type);
        }

        public static ChildPropertyChangedProcessor CreateFromPrototype(ChildPropertyChangedProcessor prototype)
        {
            return new ChildPropertyChangedProcessor(prototype.FieldTypes) {notifyChildPropertyChangedHandlers = new Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor>()};
        }
    }
}
