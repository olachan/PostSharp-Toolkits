﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.INPC
{
    /// <summary>
    /// Under development. Early version !!!
    /// </summary>
    [Serializable]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    [IntroduceInterface(typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    [IntroduceInterface(typeof(IPropagatedChange), OverrideAction = InterfaceOverrideAction.Ignore)]
    public class NotifyPropertyChangedAttribute : InstanceLevelAspect, INotifyPropertyChanged, IPropagatedChange
    {
        //TODO: cope with PS0089 warning on event backing field
        // Compile-time use only
        [NonSerialized]
        private static Lazy<PropertiesDependencieAnalyzer> analyzer = new Lazy<PropertiesDependencieAnalyzer>();

        // Used for serializing propertyDependencyMap
        private PropertyDependencySerializationStore propertyDependencySerializationStore;

        private ExplicitDependencyMap explicitDependencyMap;

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            //TODO: Consider better serialization mechanism - maybe simply introduce assembly level aspect doing the stuff below?

            //Grab the dependencies map to serialize if, if no other aspect has done it before
            if (analyzer != null)
            {
                this.propertyDependencySerializationStore = new PropertyDependencySerializationStore(analyzer.Value);
                analyzer = null;
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            //If dependencies map was serialized within this aspect, copy the data to global map
            if (this.propertyDependencySerializationStore != null)
            {
                this.propertyDependencySerializationStore.CopyToMap();
                this.propertyDependencySerializationStore = null;
            }
        }

        [NonSerialized]
        private Dictionary<string, PropagetedChangeEventHandlerDescriptor> progatedChangedHandlers;

        [OnLocationSetValueAdvice]
        [MethodPointcut("SelectFields")]
        // [MulticastPointcut(Targets = MulticastTargets.Field)]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            this.UnHookPropagatedChangedHandler(args);

            args.ProceedSetValue();

            this.HookPropagatedChangeHandler(args);

            IList<string> propertyList;
            if (FieldDependenciesMap.FieldDependentProperties.TryGetValue(args.LocationFullName, out propertyList))
            {
                PropertyChangesTracker.Accumulator.AddProperties(args.Instance, propertyList);
            }

            IPropagatedChange instance = (IPropagatedChange)this.Instance;
            instance.RaisePropagatedChange(new PropagatedChangeEventArgs(args.LocationName));
            // PropertyChangesTracker.RaisePropertyChanged(args.Instance, this.OnPropertyChangedMethod, false);
        }

        private IEnumerable<FieldInfo> SelectFields(Type type)
        {
            // Select only fields that are relevant
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where( f =>    analyzer.Value.FieldDependentProperties.ContainsKey(f.FullName()) ||
                                analyzer.Value.MethodFieldDependencies.Any(d => d.Value.Contains( f )) || //TODO verify if contains works properly
                                explicitDependencyMap.GetDependentProperties( f.Name ).Any());
        } 

        private void HookPropagatedChangeHandler(LocationInterceptionArgs args)
        {
            //TODO: verify
            IPropagatedChange currentValue = args.Value as IPropagatedChange;
            if (currentValue != null)
            {
                string locationName = args.LocationName;
                PropagetedChangeEventHandlerDescriptor handlerDescriptor =
                    new PropagetedChangeEventHandlerDescriptor(currentValue, (s, a) => this.GenericPropagatedChangeEventHandler(locationName, s, a));
                this.progatedChangedHandlers.AddOrUpdate(locationName, handlerDescriptor);
                currentValue.PropagatedChange += handlerDescriptor.Handler;
            }
        }

        private void UnHookPropagatedChangedHandler(LocationInterceptionArgs args)
        {
            //TODO: verify
            PropagetedChangeEventHandlerDescriptor handlerDescriptor;
            if (this.progatedChangedHandlers.TryGetValue(args.LocationName, out handlerDescriptor) && handlerDescriptor.Reference.IsAlive)
            {
                IPropagatedChange currentValue = handlerDescriptor.Reference.Target as IPropagatedChange;
                if (currentValue != null)
                {
                    currentValue.PropagatedChange -= handlerDescriptor.Handler;
                }
            }
        }

        private void GenericPropagatedChangeEventHandler(string locationName, object sender, PropagatedChangeEventArgs args)
        {
            //TODO: verify
            IPropagatedChange instance = (IPropagatedChange)this.Instance;

            instance.RaisePropagatedChange(new PropagatedChangeEventArgs(locationName, args));
        }

        private void SelfPropagatedChangeEventHandler(object sender, PropagatedChangeEventArgs args)
        {
            IEnumerable<string> changedProperties = this.explicitDependencyMap.GetDependentProperties(args.Path);

            PropertyChangesTracker.Accumulator.AddProperties(this.Instance, changedProperties);

            PropertyChangesTracker.RaisePropertyChanged(this.Instance, this.OnPropertyChangedMethod, false);
        }

        [OnLocationGetValueAdvice]
        [ProvideAspectRole("INPC_EventHook")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, "INPC_EventRaise")]
        [MulticastPointcut(Targets = MulticastTargets.Property)]
        public void OnPropertyGet(LocationInterceptionArgs args)
        {
            this.UnHookPropagatedChangedHandler(args);

            args.ProceedGetValue();

            this.HookPropagatedChangeHandler(args);
        }

        [OnMethodInvokeAdvice]
        [ProvideAspectRole("INPC_EventRaise")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_EventHook")]
        [MethodPointcut("SelectMethods")]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            try
            {
                PropertyChangesTracker.StackContext.PushOnStack(args.Instance);
                args.Proceed();
            }
            finally
            {
                PropertyChangesTracker.RaisePropertyChanged(args.Instance, this.OnPropertyChangedMethod, true);
            }
        }

        private IEnumerable<MethodBase> SelectMethods(Type type)
        {
            return
                type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(
                    m => !m.GetCustomAttributes(typeof(NoAutomaticPropertyChangedNotificationsAttribute), true).Any());
            //.Where(m => !(m.IsSpecialName && (m.Name.StartsWith("add_") || m.Name.StartsWith("remove_"))));
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            analyzer.Value.AnalyzeType(type);

            var properties =
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(
                    p => !p.GetCustomAttributes(typeof(NoAutomaticPropertyChangedNotificationsAttribute), true).Any()).Select(
                        p => new { Property = p, DependsOn = p.GetCustomAttributes(typeof(DependsOn), false) }).Where(p => p.DependsOn.Any());

            this.explicitDependencyMap =
                new ExplicitDependencyMap(
                    properties.Select(p => new ExplicitDependency(p.Property.Name, p.DependsOn.SelectMany(d => ((DependsOn)d).Dependencies))));
        }

        public override void RuntimeInitializeInstance()
        {
            base.RuntimeInitializeInstance();
            this.progatedChangedHandlers = new Dictionary<string, PropagetedChangeEventHandlerDescriptor>();

            ((IPropagatedChange)this.Instance).PropagatedChange += this.SelfPropagatedChangeEventHandler;
        }

        [ImportMember("OnPropertyChanged")]
        public Action<string> OnPropertyChangedMethod;

        [IntroduceMember(Visibility = Visibility.Family, IsVirtual = true, OverrideAction = MemberOverrideAction.Ignore)]
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this.Instance, new PropertyChangedEventArgs(propertyName));
            }
        }

        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropagatedChange(PropagatedChangeEventArgs args)
        {
            PropagatedChangeEventHandler handler = this.PropagatedChange;
            if (handler != null)
            {
                handler(this.Instance, args);
            }
        }

        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event PropagatedChangeEventHandler PropagatedChange;

        [Serializable]
        private class ExplicitDependencyMap
        {
            private List<ExplicitDependency> dependencies;

            public ExplicitDependencyMap(IEnumerable<ExplicitDependency> dependencies)
            {
                this.dependencies = dependencies.ToList();
            }

            public IEnumerable<string> GetDependentProperties(string changedPath)
            {
                return this.dependencies.Where(d => d.Dependencies.Any(pd => pd.StartsWith(changedPath))).Select(d => d.PropertyName);
            }
        }

        [Serializable]
        private class ExplicitDependency
        {
            public ExplicitDependency(string propertyName, IEnumerable<string> dependencies)
            {
                this.PropertyName = propertyName;
                this.Dependencies = dependencies.ToList();
            }

            public string PropertyName { get; set; }

            public List<string> Dependencies { get; set; }
        }

        [Serializable]
        private class PropagetedChangeEventHandlerDescriptor
        {
            public PropagetedChangeEventHandlerDescriptor(object reference, PropagatedChangeEventHandler handler)
            {
                this.Reference = new WeakReference(reference);
                this.Handler = handler;
            }

            public WeakReference Reference { get; private set; }

            public PropagatedChangeEventHandler Handler { get; private set; }
        }
    }

    //TODO: Rename!
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class NoAutomaticPropertyChangedNotificationsAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotentMethodAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class InstanceScopedPropertyAttribute : Attribute
    {
    }
}