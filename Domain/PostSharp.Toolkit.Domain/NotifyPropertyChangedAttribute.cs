﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

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
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict, PersistMetaData = true)]
    [IntroduceInterface(typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    [IntroduceInterface(typeof(INotifyChildPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    public class NotifyPropertyChangedAttribute : InstanceLevelAspect, INotifyPropertyChanged, INotifyChildPropertyChanged
    {
        // Compile-time use only
        [NonSerialized]
        private static Lazy<PropertiesDependencieAnalyzer> analyzer = new Lazy<PropertiesDependencieAnalyzer>();

        // Used for serializing propertyDependencyMap
        private PropertyDependencySerializationStore propertyDependencySerializationStore;

        //Dependencies built with DependsOnAttribute and derived API
        private ExplicitDependencyMap explicitDependencyMap;

        private ChildPropertyChangedProcessor childPropertyChangedProcessor;

        //camparer to compare old and new value based on field type (reference, value)
        private FieldValueComparer fieldValueComparer;

        private bool initialized = false;


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
            if (this.fieldValueComparer.AreEqual(args.LocationFullName, args.GetCurrentValue(), args.Value))
            {
                args.ProceedSetValue();
                return;
            }

            args.ProceedSetValue();

            this.childPropertyChangedProcessor.HandleFieldChange( args );

            PropertyChangesTracker.RaisePropertyChangedIfNeeded(args);
        }

        private IEnumerable<FieldInfo> SelectFields(Type type)
        {
            // Select only fields that are relevant
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => analyzer.Value.FieldDependentProperties.ContainsKey(f.FullName()) ||
                             analyzer.Value.MethodFieldDependencies.Any(d => d.Value.Contains(f)) ||
                             explicitDependencyMap.GetDependentProperties(f.Name).Any());
        }

        [OnLocationGetValueAdvice]
        [ProvideAspectRole("INPC_EventHook")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, "INPC_EventRaise")]
        [MethodPointcut("SelectProperties")]
        public void OnPropertyGet(LocationInterceptionArgs args)
        {
            args.ProceedGetValue();

            this.childPropertyChangedProcessor.HandleGetProperty(args);
        }

        private IEnumerable<PropertyInfo> SelectProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        [OnMethodInvokeAdvice]
        [ProvideAspectRole("INPC_EventRaise")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_EventHook")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_InitializersHook")]
        [MethodPointcut("SelectMethods")]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            try
            {
                PropertyChangesTracker.PushOnStack(args.Instance);
                args.Proceed();
            }
            finally
            {
                PropertyChangesTracker.PopFromStack();
                if (PropertyChangesTracker.StackPeek() != args.Instance)
                {
                    PropertyChangesTracker.RaisePropertyChanged();
                }
            }
        }

        private IEnumerable<MethodBase> SelectMethods(Type type)
        {
            return
                type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(
                    m => !m.GetCustomAttributes(typeof(NotifyPropertyChangedIgnoreAttribute), true).Any());
        }

        // hook handlers to all fields that contain not null value before constructor execution (field initializer assigned values)
        [OnMethodEntryAdvice]
        [ProvideAspectRole("INPC_InitializersHook")]
        [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, "INPC_EventRaise")]
        [MethodPointcut("SelectConstructors")]
        public void OnConstructorEntry(MethodExecutionArgs args)
        {
            if (initialized)
            {
                return;
            }
          
            this.childPropertyChangedProcessor.HookHandlersToAllFields();

            initialized = true;

        }

        private IEnumerable<MethodBase> SelectConstructors(Type type)
        {
            return type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        }

        public override bool CompileTimeValidate(Type type)
        {
            if (!(type.BaseType != null && type.BaseType.GetCustomAttributes( true ).Any(a => a is NotifyPropertyChangedAttribute )))
            {
                if (!typeof(INotifyChildPropertyChanged).IsAssignableFrom(type) && typeof(INotifyPropertyChanged).IsAssignableFrom(type))
                {
                    DomainMessageSource.Instance.Write(type, SeverityType.Error, "INPC005", type.FullName);
                    return false;
                }

                if (typeof(INotifyChildPropertyChanged).IsAssignableFrom(type.BaseType) || typeof(INotifyPropertyChanged).IsAssignableFrom(type.BaseType))
                {
                    DomainMessageSource.Instance.Write(type, SeverityType.Error, "INPC006", type.FullName);
                    return false;
                }
            }
            
            return base.CompileTimeValidate(type);
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            analyzer.Value.AnalyzeType(type);

            this.explicitDependencyMap = ExplicitDependencyAnalyzer.Analyze(type);
            this.fieldValueComparer = new FieldValueComparer( type );
            this.childPropertyChangedProcessor = ChildPropertyChangedProcessor.CompileTimeCreate(type, analyzer.Value.MethodFieldDependencies, this.fieldValueComparer, this.explicitDependencyMap);
        }

        public override void RuntimeInitializeInstance()
        {
            base.RuntimeInitializeInstance();
            childPropertyChangedProcessor.RuntimeInitialize();
        }

        public override object CreateInstance(AdviceArgs adviceArgs)
        {
            NotifyPropertyChangedAttribute clone = (NotifyPropertyChangedAttribute)base.CreateInstance(adviceArgs);
            clone.initialized = false;
            clone.childPropertyChangedProcessor = ChildPropertyChangedProcessor.CreateFromPrototype(this.childPropertyChangedProcessor, adviceArgs.Instance);

            return clone;
        }

        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this.Instance, new PropertyChangedEventArgs(propertyName));
            }
        }

        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event EventHandler<NotifyChildPropertyChangedEventArgs> ChildPropertyChanged;

        public void RaiseChildPropertyChanged(NotifyChildPropertyChangedEventArgs args)
        {
            EventHandler<NotifyChildPropertyChangedEventArgs> handler = this.ChildPropertyChanged;
            if (handler != null)
            {
                handler(this.Instance, args);
            }
        }
    }
}