using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using PostSharp.Aspects;

namespace PostSharp.Toolkit.Domain
{
    [Serializable]
    internal sealed class ChildPropertyChangedProcessor
    {
        [NonSerialized]
        private Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor> notifyChildPropertyChangedHandlers;

        public Dictionary<string, bool> FieldIsValueType { get; private set; }

        private ChildPropertyChangedProcessor( Dictionary<string, bool> fieldIsValueType )
        {
            this.FieldIsValueType = fieldIsValueType;
        }

        private ChildPropertyChangedProcessor( Type type )
        {
            this.CompileTimeInitialize( type );
        }

        private void CompileTimeInitialize( Type type )
        {
            var fieldTypes = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToDictionary(f => f.FullName(), f => f.FieldType.IsValueType);

            FieldIsValueType = fieldTypes.Union(
                type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToDictionary(
                    f => f.FullName(), f => f.PropertyType.IsValueType)).ToDictionary(kv => kv.Key, kv => kv.Value);
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
            bool isValueType;
            this.FieldIsValueType.TryGetValue(locationFullName, out isValueType);

            return isValueType ? !Equals(currentValue, newValue) : !ReferenceEquals(currentValue, newValue);
        }

        private void GenericNotifyChildPropertyChangedEventHandler(string locationName, object instance, object sender, NotifyChildPropertyChangedEventArgs args)
        {
            INotifyChildPropertyChanged incpc = (INotifyChildPropertyChanged)instance;

            incpc.RaisePropagatedChange(new NotifyChildPropertyChangedEventArgs(locationName, args));
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
            return new ChildPropertyChangedProcessor(prototype.FieldIsValueType) {notifyChildPropertyChangedHandlers = new Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor>()};
        }
    }
}
