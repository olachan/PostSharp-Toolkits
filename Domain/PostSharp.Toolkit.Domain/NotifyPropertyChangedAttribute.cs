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
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Constraints;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Under development. Early version !!!
    /// </summary>
    [Serializable]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict, PersistMetaData = true)]
    public class NotifyPropertyChangedAttribute : InstanceLevelAspect, IAspectProvider
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

        public override bool CompileTimeValidate(Type type)
        {
            if (typeof(INotifyPropertyChanged).IsAssignableFrom(type))
            {
                var onPropertyChangedMethod = type.GetMethod( "OnPropertyChanged", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                    null , new[] {typeof(string)}, null );
                if (onPropertyChangedMethod == null || onPropertyChangedMethod.ReturnType != typeof(void))
                {
                    DomainMessageSource.Instance.Write( type, SeverityType.Error, "INPC007", type.FullName);
                }
            }

            //TODO: Validate to make sure PostsharpToolkitsDomain_ChildPropertyChanged method and event are not there (so that we do not get naming conflicts)
            
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
            // ((INotifyChildPropertyChanged)this.Instance).ChildPropertyChanged += this.ChildPropertyChangedEventHandler;
        }

        public override object CreateInstance(AdviceArgs adviceArgs)
        {
            NotifyPropertyChangedAttribute clone = (NotifyPropertyChangedAttribute)base.CreateInstance(adviceArgs);
            clone.childPropertyChangedProcessor = ChildPropertyChangedProcessor.CreateFromPrototype(this.childPropertyChangedProcessor, adviceArgs.Instance);

            return clone;
        }

        IEnumerable<AspectInstance> IAspectProvider.ProvideAspects( object targetElement )
        {
            //We may be adding those aspects redundantly but they're not going to have any effect anyway
            Type type = (Type) targetElement;
            if (!typeof(INotifyPropertyChanged).IsAssignableFrom(type))
            {
                yield return new AspectInstance(type, new ObjectConstruction(typeof(IntroduceNotifyPropertyChangedAttribute)), null);
            }
            yield return new AspectInstance(type, new ObjectConstruction(typeof(IntroduceNotifyChildPropertyChangedAttribute)), null);
            
            //if (!typeof(INotifyPropertyChanged).IsAssignableFrom( type ) &&
            //    !type.GetCustomAttributes( typeof(IntroduceNotifyPropertyChangedAttribute), true ).Any())
            //{
            //    yield return new AspectInstance( type, new IntroduceNotifyPropertyChangedAttribute() );
            //}
            //if (!type.GetCustomAttributes( typeof(IntroduceNotifyChildPropertyChangedAttribute), true ).Any())
            //{
            //    yield return new AspectInstance( type, new IntroduceNotifyChildPropertyChangedAttribute() );
            //}
        }



        #region Inner attributes (events introduction)

        //TODO: Make sure they're applied only once; consider building expressions for NotifyPropertyChangedAccessor in compile-time & serializing them

        /// <summary>
        /// Aspect introducing INPC interface with OnPropertyChanged method
        /// </summary>
        [Internal]
        [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
        [MulticastAttributeUsage(MulticastTargets.Class, PersistMetaData = false, AllowMultiple = false, Inheritance = MulticastInheritance.None)]
        [IntroduceInterface(typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Fail)]
        public class IntroduceNotifyPropertyChangedAttribute : InstanceLevelAspect, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore, Visibility = Visibility.Family)]
            public void OnPropertyChanged(string propertyName)
            {
                PropertyChangedEventHandler handler = this.PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Aspect introducing INPC interface with OnPropertyChanged method
        /// </summary>
        [Internal]
        [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
        [MulticastAttributeUsage(MulticastTargets.Class, PersistMetaData = false, AllowMultiple = false, Inheritance = MulticastInheritance.None)]
        public class IntroduceNotifyChildPropertyChangedAttribute : InstanceLevelAspect
        {
            [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore, Visibility = Visibility.Family)]
            public event EventHandler<ChildPropertyChangedEventArgs> PostSharpToolkitsDomain_ChildPropertyChanged;


            [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore, Visibility = Visibility.Family)]
            public void PostSharpToolkitsDomain_OnChildPropertyChanged(string propertyPath)
            {
                EventHandler<ChildPropertyChangedEventArgs> handler = this.PostSharpToolkitsDomain_ChildPropertyChanged;
                if (handler != null) handler(this, new ChildPropertyChangedEventArgs(propertyPath));
            }
        }

        #endregion
    }
}