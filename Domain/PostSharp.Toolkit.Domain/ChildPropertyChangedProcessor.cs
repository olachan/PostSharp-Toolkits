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

        private readonly ExplicitDependencyMap explicitDependencyMap;

        // map connecting property to field if property depends exactly on one field. Moreover return types of property and field match.
        private readonly PropertyFieldBindingsMap propertyToFieldBindings;

        private readonly object instance;

        private ChildPropertyChangedProcessor(ChildPropertyChangedProcessor prototype, object instance)
        {
            this.instance = instance;
            this.fieldValueComparer = prototype.fieldValueComparer;
            this.explicitDependencyMap = prototype.explicitDependencyMap;
            this.propertyToFieldBindings = PropertyFieldBindingsMap.CreateFromPrototype(prototype.propertyToFieldBindings);
            this.notifyChildPropertyChangedHandlers = new Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor>();
        }

        private ChildPropertyChangedProcessor(
            PropertyFieldBindingsMap propertyToFieldBindings, 
            FieldValueComparer fieldValueComparer, 
            ExplicitDependencyMap explicitDependencyMap)
        {
            this.fieldValueComparer = fieldValueComparer;
            this.explicitDependencyMap = explicitDependencyMap;
            this.propertyToFieldBindings = propertyToFieldBindings;
        }

       

        // Initialize at runtime - compile field getters. Can't be done compile time becouse generated code is not serializable
        public void RuntimeInitialize()
        {
            this.propertyToFieldBindings.RuntimeInitialize();
        }

        public void HandleGetProperty(LocationInterceptionArgs args)
        {
            PropertyFieldBinding sourceField;
            // TODO if there is no binding for the property we should scan all fields with return type matching return type of property and add binding if posible
            // try find source field for property
            if (this.propertyToFieldBindings.TryGetSourceFieldBinding(args.LocationName, out sourceField))
            {
                object value = sourceField.Field.GetValue(args.Instance);

                if (fieldValueComparer.AreEqual(args.LocationFullName, value, args.Value))
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

        public void HandleFieldChange(LocationInterceptionArgs args)
        {
            List<string> propertyList;
            if (FieldDependenciesMap.FieldDependentProperties.TryGetValue(args.LocationFullName, out propertyList))
            {
                PropertyChangesTracker.StoreChangedProperties(this.instance, propertyList);
            }

            if (propertyList == null)
            {
                propertyList = new List<string>();
            }
            else
            {
                propertyList = propertyList.ToList();
            }

            propertyList.Add(args.LocationName);

            this.ChildPropertyChanged(propertyList);
            this.ReHookNotifyChildPropertyChangedHandler(args);
        }

        private void NotifyChildPropertyChangedEventHandler(string locationName, NotifyChildPropertyChangedEventArgs args)
        {
            this.ChildPropertyChanged(new List<string> { string.Format("{0}.{1}", locationName, args.Path) });
        }

        private void ChildPropertyChanged(List<string> paths)
        {
            PropertyChangesTracker.StoreChangedChildProperties(this.instance, paths);

            do
            {
                List<string> changedProperties = paths.SelectMany(this.explicitDependencyMap.GetDependentProperties).ToList();

                PropertyChangesTracker.StoreChangedProperties(this.instance, changedProperties);

                paths = paths.SelectMany(this.GetAffectedPaths).Union(changedProperties).ToList();

                PropertyChangesTracker.StoreChangedChildProperties(this.instance, paths.ToList());
            }
            while (paths.Count > 0);
        }

        // get properties affected by the change described in path
        private IEnumerable<string> GetAffectedPaths(string path)
        {
            int dotIndex = path.IndexOf('.');
            string changedField;
            string changedPath = null;

            if (dotIndex == -1)
            {
                changedField = path;
            }
            else
            {
                changedField = path.Substring(0, dotIndex);
                changedPath = path.Substring(dotIndex + 1);
            }

            return this.propertyToFieldBindings.GetDependentPropertiesBindings(changedField)
                .Select(d => dotIndex == -1 ? d.PropertyName : string.Format("{0}.{1}", d.PropertyName, changedPath));
        }

        private void ReHookNotifyChildPropertyChangedHandler(LocationInterceptionArgs args)
        {
            NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor;

            if (this.notifyChildPropertyChangedHandlers.TryGetValue(args.LocationName, out handlerDescriptor) && handlerDescriptor.Reference.IsAlive)
            {
                if (!this.fieldValueComparer.AreEqual(args.LocationFullName, args.Value, handlerDescriptor.Reference))
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
                this.UnHookNotifyChildPropertyChangedHandler(handlerDescriptor);
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

        private void HookNotifyChildPropertyChangedHandler(LocationInterceptionArgs args)
        {
            INotifyChildPropertyChanged currentValue = args.Value as INotifyChildPropertyChanged;
            if (currentValue != null)
            {
                string locationName = args.LocationName;
                NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor =
                    new NotifyChildPropertyChangedEventHandlerDescriptor(currentValue, (_, a) => this.NotifyChildPropertyChangedEventHandler(locationName, a));
                this.notifyChildPropertyChangedHandlers.AddOrUpdate(locationName, handlerDescriptor);
                currentValue.ChildPropertyChanged += handlerDescriptor.Handler;
            }
        }

        public static ChildPropertyChangedProcessor CompileTimeCreate(Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies, FieldValueComparer fieldValueComparer, ExplicitDependencyMap explicitDependencyMap)
        {
            PropertyFieldBindingsMap propertyToFieldBindings = PropertyToFieldBindingGenerator.GenerateBindings(type, methodFieldDependencies);
            return new ChildPropertyChangedProcessor(propertyToFieldBindings, fieldValueComparer, explicitDependencyMap);
        }

        public static ChildPropertyChangedProcessor CreateFromPrototype(ChildPropertyChangedProcessor prototype, object instance)
        {
            return new ChildPropertyChangedProcessor(prototype, instance);
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

        private static class PropertyToFieldBindingGenerator
        {
            public static PropertyFieldBindingsMap GenerateBindings(Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies)
            {
                // build propertyToFieldBindings
                PropertyFieldBindingsMap propertyToFieldBindings = new PropertyFieldBindingsMap();

                var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo propertyInfo in allProperties)
                {
                    // TODO should add binding even if there are multiple fields that property depends on but only one with matching type
                    IList<FieldInfo> fieldList;
                    if (methodFieldDependencies.TryGetValue(propertyInfo.GetGetMethod(), out fieldList) &&
                        fieldList.Count == 1 &&
                        propertyInfo.PropertyType == fieldList.First().FieldType)
                    {
                        propertyToFieldBindings.AddBinding(propertyInfo.Name, fieldList.Single(), type);
                    }
                }
                return propertyToFieldBindings;
            }
        }
    }
}
