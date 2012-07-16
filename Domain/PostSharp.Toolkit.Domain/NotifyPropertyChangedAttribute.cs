#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
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

        private ChildPropertyChangedProcessor childPropertyChangedProcessor;


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

        [OnLocationSetValueAdvice]
        [MethodPointcut("SelectFields")]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            if (!this.childPropertyChangedProcessor.IsValueChanged(args.LocationFullName, args.GetCurrentValue(), args.Value))
            {
                args.ProceedSetValue();
                return;
            }

            args.ProceedSetValue();

            this.childPropertyChangedProcessor.ReHookNotifyChildPropertyChangedHandler(args);

            IList<string> propertyList;
            if (FieldDependenciesMap.FieldDependentProperties.TryGetValue(args.LocationFullName, out propertyList))
            {
                PropertyChangesTracker.Accumulator.AddProperties(args.Instance, propertyList);
            }

            INotifyChildPropertyChanged instance = (INotifyChildPropertyChanged)this.Instance;
            instance.RaisePropagatedChange(new NotifyChildPropertyChangedEventArgs(args.LocationName));
        }

        private IEnumerable<FieldInfo> SelectFields(Type type)
        {
            // Select only fields that are relevant
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => analyzer.Value.FieldDependentProperties.ContainsKey(f.FullName()) ||
                             analyzer.Value.MethodFieldDependencies.Any(d => d.Value.Contains(f)) ||
                             explicitDependencyMap.GetDependentProperties(f.Name).Any());
        }

        private void ChildPropertyChangedEventHandler(object sender, NotifyChildPropertyChangedEventArgs args)
        {
            IEnumerable<string> changedProperties = this.explicitDependencyMap.GetDependentProperties(args.Path);

            PropertyChangesTracker.Accumulator.AddProperties(this.Instance, changedProperties);

            PropertyChangesTracker.RaisePropertyChanged(this.Instance, this.OnPropertyChangedMethod, false);


            var paths = this.childPropertyChangedProcessor.GetEffectedPaths(args);

            foreach (string path in paths)
            {
                this.RaisePropagatedChange(new NotifyChildPropertyChangedEventArgs(path));
            }
        }

        [OnLocationGetValueAdvice]
        [ProvideAspectRole("INPC_EventHook")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, "INPC_EventRaise")]
        [MulticastPointcut(Targets = MulticastTargets.Property)]
        public void OnPropertyGet(LocationInterceptionArgs args)
        {
            args.ProceedGetValue();

            this.childPropertyChangedProcessor.ProcessGet( args );
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

            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

           

            var properties = allProperties
                .Where(p => !p.GetCustomAttributes(typeof(NotifyPropertyChangedIgnoreAttribute), true).Any())
                .Select(p => new { Property = p, DependsOn = p.GetCustomAttributes(typeof(DependsOnAttribute), false) })
                .Where(p => p.DependsOn.Any());

            this.explicitDependencyMap =
                new ExplicitDependencyMap(
                    properties.Select(p => new ExplicitDependency(p.Property.Name, p.DependsOn.SelectMany(d => ((DependsOnAttribute)d).Dependencies))));

            this.childPropertyChangedProcessor = ChildPropertyChangedProcessor.CompileTimeCreate(type, analyzer.Value.MethodFieldDependencies);
        }

        public override void RuntimeInitializeInstance()
        {
            base.RuntimeInitializeInstance();
            childPropertyChangedProcessor.RuntimeInitialize();
            ((INotifyChildPropertyChanged)this.Instance).ChildPropertyChanged += this.ChildPropertyChangedEventHandler;
        }

        public override object CreateInstance(AdviceArgs adviceArgs)
        {
            NotifyPropertyChangedAttribute clone = (NotifyPropertyChangedAttribute)base.CreateInstance(adviceArgs);
            clone.childPropertyChangedProcessor = ChildPropertyChangedProcessor.CreateFromPrototype(this.childPropertyChangedProcessor);

            return clone;
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

        
    }
}