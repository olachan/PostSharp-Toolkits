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
        // collection of handelers attached to objects. Maintained to unhook handler when no longer needed.
        [NonSerialized]
        private Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor> notifyChildPropertyChangedHandlers;

        // comparer to compare old and new value based on field type (reference, value)
        private readonly FieldValueComparer fieldValueComparer;

        // map connecting property to field if property depends exactly on one field. Moreover return types of property and field match.
        private PropertyFieldBindingsMap propertyFieldBindings;

        private ChildPropertyChangedProcessor(ChildPropertyChangedProcessor prototype)
        {
            this.fieldValueComparer = prototype.fieldValueComparer;
            this.propertyFieldBindings = PropertyFieldBindingsMap.CreateFromPrototype( prototype.propertyFieldBindings );
        }

        private ChildPropertyChangedProcessor(Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies, FieldValueComparer fieldValueComparer)
        {
            this.fieldValueComparer = fieldValueComparer;
            this.CompileTimeInitialize(type, methodFieldDependencies);
        }

        private void CompileTimeInitialize( Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies )
        {
            // build propertyFieldBindings
            this.propertyFieldBindings = new PropertyFieldBindingsMap();

            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo propertyInfo in allProperties)
            {
                IList<FieldInfo> fieldList;
                if (methodFieldDependencies.TryGetValue(propertyInfo.GetGetMethod(), out fieldList) &&
                    fieldList.Count == 1 &&
                    propertyInfo.PropertyType == fieldList.First().FieldType)
                {
                    this.propertyFieldBindings.AddBinding(propertyInfo.Name, fieldList.Single(), type);
                }
            }
        }

        // get properties affected by the change described in args
        public IEnumerable<string> GetAffectedPaths(NotifyChildPropertyChangedEventArgs args)
        {
            int dotIndex = args.Path.IndexOf('.');
            if (dotIndex == -1)
            {
                return Enumerable.Empty<string>();
            }

            string changedField = args.Path.Substring(0, dotIndex);
            string changedPath = args.Path.Substring(dotIndex + 1);
            return this.propertyFieldBindings.GetDependentPropertiesBindings(changedField)
                .Select(d => string.Format("{0}.{1}", d.PropertyName, changedPath));
        }

        // Initialize at runtime - compile field getters. Can't be done compile time becouse generated code is not serializable
        public void RuntimeInitialize()
        {
            this.propertyFieldBindings.RuntimeInitialize();
        }

        public void ProcessGet(LocationInterceptionArgs args)
        {
            PropertyFieldBinding sourceField;
            // try find source field for property
            if (this.propertyFieldBindings.TryGetSourceFieldBinding(args.LocationName, out sourceField))
            {
                object value = sourceField.Field.GetValue(args.Instance);

                if (ReferenceEquals(value, args.Value))
                {
                    // field and property values are equal, mark source field binding as active and if there is a handler hooked to the property un hook it
                    sourceField.IsActive = true;
                    UnHookNotifyChildPropertyChangedHandler(args);
                }
                else
                {
                    // field and property values differ, mark source field binding as inactive and re hook a handler to the property
                    sourceField.IsActive = false;
                    ReHookNotifyChildPropertyChangedHandler(args);
                }
            }
            else
            {
                // no source field binding just re hook property handler
                ReHookNotifyChildPropertyChangedHandler(args);
            }
        }

        public void ReHookNotifyChildPropertyChangedHandler(LocationInterceptionArgs args)
        {
            NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor;
            
            if (this.notifyChildPropertyChangedHandlers.TryGetValue(args.LocationName, out handlerDescriptor) && handlerDescriptor.Reference.IsAlive)
            {
                if (this.fieldValueComparer.IsValueChanged(args.LocationFullName, args.Value, handlerDescriptor.Reference))
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

       

        public static ChildPropertyChangedProcessor CompileTimeCreate(Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies, FieldValueComparer fieldValueComparer)
        {
            return new ChildPropertyChangedProcessor(type, methodFieldDependencies, fieldValueComparer);
        }

        public static ChildPropertyChangedProcessor CreateFromPrototype(ChildPropertyChangedProcessor prototype)
        {
            return new ChildPropertyChangedProcessor(prototype) { notifyChildPropertyChangedHandlers = new Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor>() };
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
    }
}
