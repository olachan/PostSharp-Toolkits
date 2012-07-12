#region Copyright (c) 2012 by SharpCrafters s.r.o.

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

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Under development. Early version !!!
    /// </summary>
    [Serializable]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    [IntroduceInterface(typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    [IntroduceInterface(typeof(INotifyChildPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    public class NotifyPropertyChangedAttribute : InstanceLevelAspect, INotifyPropertyChanged, INotifyChildPropertyChanged
    {
        // Compile-time use only
        [NonSerialized]
        private static Lazy<PropertiesDependencieAnalyzer> analyzer = new Lazy<PropertiesDependencieAnalyzer>();

        // Used for serializing propertyDependencyMap
        private PropertyDependencySerializationStore propertyDependencySerializationStore;

        private ExplicitDependencyMap explicitDependencyMap;

        private Dictionary<string, bool> fieldIsValueType;
            
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
        private Dictionary<string, PropagetedChangeEventHandlerDescriptor> propagatedChangedHandlers;

        [OnLocationSetValueAdvice]
        [MethodPointcut("SelectFields")]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            if ( !this.IsValueChanged( args ) )
            {
                args.ProceedSetValue();
                return;
            }

            this.UnHookPropagatedChangedHandler(args);

            args.ProceedSetValue();

            this.HookPropagatedChangeHandler(args);

            IList<string> propertyList;
            if (FieldDependenciesMap.FieldDependentProperties.TryGetValue(args.LocationFullName, out propertyList))
            {
                PropertyChangesTracker.Accumulator.AddProperties(args.Instance, propertyList);
            }

            INotifyChildPropertyChanged instance = (INotifyChildPropertyChanged)this.Instance;
            instance.RaisePropagatedChange(new NotifyChildPropertyChangedEventArgs(args.LocationName));
        }

        private bool IsValueChanged( LocationInterceptionArgs args )
        {
            bool isValueType;
            this.fieldIsValueType.TryGetValue( args.LocationFullName, out isValueType );

            return isValueType ? !Equals( args.GetCurrentValue(), args.Value ) : !ReferenceEquals( args.GetCurrentValue(), args.Value );
        }

        private IEnumerable<FieldInfo> SelectFields(Type type)
        {
            // Select only fields that are relevant
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where( f => analyzer.Value.FieldDependentProperties.ContainsKey(f.FullName()) ||
                             analyzer.Value.MethodFieldDependencies.Any(d => d.Value.Contains( f )) ||
                             explicitDependencyMap.GetDependentProperties( f.Name ).Any());
        } 

        private void HookPropagatedChangeHandler(LocationInterceptionArgs args)
        {
            INotifyChildPropertyChanged currentValue = args.Value as INotifyChildPropertyChanged;
            if (currentValue != null)
            {
                string locationName = args.LocationName;
                PropagetedChangeEventHandlerDescriptor handlerDescriptor =
                    new PropagetedChangeEventHandlerDescriptor(currentValue, (s, a) => this.GenericPropagatedChangeEventHandler(locationName, s, a));
                this.propagatedChangedHandlers.AddOrUpdate(locationName, handlerDescriptor);
                currentValue.ChildPropertyChanged += handlerDescriptor.Handler;
            }
        }

        private void UnHookPropagatedChangedHandler(LocationInterceptionArgs args)
        {
            PropagetedChangeEventHandlerDescriptor handlerDescriptor;
            if (this.propagatedChangedHandlers.TryGetValue(args.LocationName, out handlerDescriptor) && handlerDescriptor.Reference.IsAlive)
            {
                INotifyChildPropertyChanged currentValue = handlerDescriptor.Reference.Target as INotifyChildPropertyChanged;
                if (currentValue != null)
                {
                    currentValue.ChildPropertyChanged -= handlerDescriptor.Handler;
                }
            }
        }

        private void GenericPropagatedChangeEventHandler(string locationName, object sender, NotifyChildPropertyChangedEventArgs args)
        {
            INotifyChildPropertyChanged instance = (INotifyChildPropertyChanged)this.Instance;

            instance.RaisePropagatedChange(new NotifyChildPropertyChangedEventArgs(locationName, args));
        }

        private void SelfChildPropertyChangedEventHandler(object sender, NotifyChildPropertyChangedEventArgs args)
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
                    m => !m.GetCustomAttributes(typeof(NotifyPropertyChangedIgnoreAttribute), true).Any());
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            analyzer.Value.AnalyzeType(type);

            var properties =
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(
                    p => !p.GetCustomAttributes(typeof(NotifyPropertyChangedIgnoreAttribute), true).Any()).Select(
                        p => new { Property = p, DependsOn = p.GetCustomAttributes(typeof(DependsOnAttribute), false) }).Where(p => p.DependsOn.Any());

            this.explicitDependencyMap =
                new ExplicitDependencyMap(
                    properties.Select(p => new ExplicitDependency(p.Property.Name, p.DependsOn.SelectMany(d => ((DependsOnAttribute)d).Dependencies))));

            this.fieldIsValueType =  type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly )
                .ToDictionary( f => f.FullName(), f => f.FieldType.IsValueType );
        }

        public override void RuntimeInitializeInstance()
        {
            base.RuntimeInitializeInstance();
            this.propagatedChangedHandlers = new Dictionary<string, PropagetedChangeEventHandlerDescriptor>();

            ((INotifyChildPropertyChanged)this.Instance).ChildPropertyChanged += this.SelfChildPropertyChangedEventHandler;
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

        public void RaisePropagatedChange(NotifyChildPropertyChangedEventArgs args)
        {
            EventHandler<NotifyChildPropertyChangedEventArgs> handler = this.ChildPropertyChanged;
            if (handler != null)
            {
                handler(this.Instance, args);
            }
        }

        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event EventHandler<NotifyChildPropertyChangedEventArgs> ChildPropertyChanged;

        [Serializable]
        private sealed class ExplicitDependencyMap
        {
            private readonly List<ExplicitDependency> dependencies;

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
        private sealed class ExplicitDependency
        {
            public ExplicitDependency(string propertyName, IEnumerable<string> dependencies)
            {
                this.PropertyName = propertyName;
                this.Dependencies = dependencies.ToList();
            }

            public string PropertyName { get; private set; }

            public List<string> Dependencies { get; private set; }
        }

        [Serializable]
        private sealed class PropagetedChangeEventHandlerDescriptor
        {
            public PropagetedChangeEventHandlerDescriptor(object reference, EventHandler<NotifyChildPropertyChangedEventArgs> handler)
            {
                this.Reference = new WeakReference(reference);
                this.Handler = handler;
            }

            public WeakReference Reference { get; private set; }

            public EventHandler<NotifyChildPropertyChangedEventArgs> Handler { get; private set; }
        }
    }

   
}