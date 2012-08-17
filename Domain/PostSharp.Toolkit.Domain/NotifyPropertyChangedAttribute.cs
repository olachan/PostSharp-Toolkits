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
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Constraints;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Toolkit.Domain.PropertyChangeTracking;
using PostSharp.Toolkit.Domain.PropertyDependencyAnalisys;
using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Custom attribute that, specifies that class decorated with this attribute should implement <see cref="INotifyPropertyChanged"/>. 
    /// If instrumented class does not implement <see cref="INotifyPropertyChanged"/> implementation will be introduced automatically.
    /// Class will processed by automatic notification mechanism and all event will be raised automatically.
    /// </summary>
    [Serializable]
    [MulticastAttributeUsage( MulticastTargets.Class, Inheritance = MulticastInheritance.Strict, PersistMetaData = true )]
    [ProvideAspectRole( StandardRoles.DataBinding )]
    [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.After, StandardRoles.Tracing )]
    [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.After, StandardRoles.Threading )]
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

        private bool initialized;

        [OnSerializing]
        public void OnSerializing( StreamingContext context )
        {
            //TODO: Consider better serialization mechanism

            //Grab the dependencies map to serialize if, if no other aspect has done it before
            if ( analyzer != null )
            {
                this.propertyDependencySerializationStore = new PropertyDependencySerializationStore( analyzer.Value );
                analyzer = null;
            }
        }

        [OnDeserialized]
        public void OnDeserialized( StreamingContext context )
        {
            //If dependencies map was serialized within this aspect, copy the data to global map
            if ( this.propertyDependencySerializationStore != null )
            {
                this.propertyDependencySerializationStore.CopyToMap();

                this.propertyDependencySerializationStore = null;
            }
        }

        [OnLocationSetValueAdvice]
        [MethodPointcut( "SelectFields" )]
        public void OnFieldSet( LocationInterceptionArgs args )
        {
            if ( this.fieldValueComparer.AreEqual( args.LocationFullName, args.GetCurrentValue(), args.Value ) )
            {
                args.ProceedSetValue();
                return;
            }

            args.ProceedSetValue();

            this.childPropertyChangedProcessor.HandleFieldChange( args );

            PropertyChangesTracker.RaisePropertyChangedIfNeeded( args );
        }

        private IEnumerable<FieldInfo> SelectFields( Type type )
        {
            // Select only fields that are relevant
            return
                type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).Where(
                    f =>
                    analyzer.Value.FieldDependentProperties.ContainsKey( f.FullName() ) ||
                    analyzer.Value.MethodFieldDependencies.Any( d => d.Value.Contains( f ) ) ||
                    this.explicitDependencyMap.GetDependentProperties( f.Name ).Any() );
        }

        [OnLocationGetValueAdvice]
        [ProvideAspectRole( "INPC_EventHook" )]
        [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, "INPC_EventRaise" )]
        [MethodPointcut( "SelectProperties" )]
        public void OnPropertyGet( LocationInterceptionArgs args )
        {
            args.ProceedGetValue();

            this.childPropertyChangedProcessor.HandleGetProperty( args );
        }

        private IEnumerable<PropertyInfo> SelectProperties( Type type )
        {
            return type.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly );
        }

        [OnMethodInvokeAdvice]
        [ProvideAspectRole( "INPC_EventRaise" )]
        [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_EventHook" )]
        [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.After, "INPC_InitializersHook" )]
        [MethodPointcut( "SelectMethods" )]
        public void OnMethodInvoke( MethodInterceptionArgs args )
        {
            try
            {
                PropertyChangesTracker.PushOnStack( args.Instance );
                args.Proceed();
            }
            finally
            {
                PropertyChangesTracker.PopFromStack();
                if ( PropertyChangesTracker.StackPeek() != args.Instance )
                {
                    PropertyChangesTracker.RaisePropertyChanged( args.Instance );
                }
            }
        }

        private IEnumerable<MethodBase> SelectMethods( Type type )
        {
            return
                type.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ).Where(
                    m => !m.GetCustomAttributes( typeof(NotifyPropertyChangedIgnoreAttribute), true ).Any() );
        }

        // hook handlers to all fields that contain not null value before constructor execution (field initializer assigned values)
        [OnMethodEntryAdvice]
        [ProvideAspectRole( "INPC_InitializersHook" )]
        [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, "INPC_EventRaise" )]
        [MethodPointcut( "SelectConstructors" )]
        public void OnConstructorEntry( MethodExecutionArgs args )
        {
            if ( this.initialized )
            {
                return;
            }

            this.childPropertyChangedProcessor.HookHandlersToAllFields();

            this.initialized = true;
        }

        private IEnumerable<MethodBase> SelectConstructors( Type type )
        {
            return type.GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
        }

        public override bool CompileTimeValidate( Type type )
        {
            // TODO: build tests
            if ( typeof(INotifyPropertyChanged).IsAssignableFrom( type ) )
            {
                MethodInfo onPropertyChangedMethod = type.GetMethod(
                    "OnPropertyChanged", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null );
                if ( onPropertyChangedMethod == null || onPropertyChangedMethod.ReturnType != typeof(void) )
                {
                    DomainMessageSource.Instance.Write( type, SeverityType.Error, "INPC008", type.FullName );
                }
            }

            if ( type.BaseType != null && !type.BaseType.IsDefined( typeof(NotifyPropertyChangedAttribute), true ) )
            {
                MethodBase forbidenMethod = type.GetMethod( "____PostSharpToolkitsDomain_OnChildPropertyChanged____", BindingFlagsSet.AllMembers );
                ParameterInfo[] parameters = forbidenMethod != null ? forbidenMethod.GetParameters() : null;

                if ( (forbidenMethod != null && parameters.Length == 1 && parameters[0].ParameterType == typeof(string)) ||
                     type.GetEvent( "____PostSharpToolkitsDomain_ChildPropertyChanged____", BindingFlagsSet.AllMembers ) != null )
                {
                    DomainMessageSource.Instance.Write( type, SeverityType.Error, "INPC009", type.FullName );
                }
            }

            return base.CompileTimeValidate( type );
        }

        public override void CompileTimeInitialize( Type type, AspectInfo aspectInfo )
        {
            this.explicitDependencyMap = analyzer.Value.AnalyzeType( type );
            this.fieldValueComparer = new FieldValueComparer( type );
            this.childPropertyChangedProcessor = ChildPropertyChangedProcessor.CompileTimeCreate(
                type, analyzer.Value.MethodFieldDependencies, this.fieldValueComparer, this.explicitDependencyMap );
        }

        public override void RuntimeInitializeInstance()
        {
            base.RuntimeInitializeInstance();
            this.childPropertyChangedProcessor.RuntimeInitialize();
        }

        public override object CreateInstance( AdviceArgs adviceArgs )
        {
            NotifyPropertyChangedAttribute clone = (NotifyPropertyChangedAttribute)base.CreateInstance( adviceArgs );
            clone.initialized = false;
            clone.childPropertyChangedProcessor = ChildPropertyChangedProcessor.CreateFromPrototype( this.childPropertyChangedProcessor, adviceArgs.Instance );

            return clone;
        }

        IEnumerable<AspectInstance> IAspectProvider.ProvideAspects( object targetElement )
        {
            Type type = (Type)targetElement;

            if ( type.BaseType != null && type.BaseType.IsDefined( typeof(NotifyPropertyChangedAttribute), true ) )
            {
                yield break;
            }

            if ( !typeof(INotifyPropertyChanged).IsAssignableFrom( type ) )
            {
                yield return new AspectInstance( type, new ObjectConstruction( typeof(IntroduceNotifyPropertyChangedAttribute) ), null );
            }
            yield return new AspectInstance( type, new ObjectConstruction( typeof(IntroduceNotifyChildPropertyChangedAttribute) ), null );
        }

        #region Inner attributes (events introduction)

        //TODO: (v2) consider building expressions for NotifyPropertyChangedAccessor in compile-time & serializing them

        /// <summary>
        /// Aspect introducing INPC interface with OnPropertyChanged method
        /// </summary>
        [Internal]
        [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
        [MulticastAttributeUsage( MulticastTargets.Class, PersistMetaData = false, AllowMultiple = false, Inheritance = MulticastInheritance.None )]
        [IntroduceInterface( typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore,
            AncestorOverrideAction = InterfaceOverrideAction.Fail )]
        public class IntroduceNotifyPropertyChangedAttribute : InstanceLevelAspect, INotifyPropertyChanged
        {
            ////TODO build tests 

            public event PropertyChangedEventHandler PropertyChanged;

            [IntroduceMember( OverrideAction = MemberOverrideAction.Fail, Visibility = Visibility.Family )]
            public void OnPropertyChanged( string propertyName )
            {
                PropertyChangedEventHandler handler = this.PropertyChanged;
                if ( handler != null )
                {
                    handler( this.Instance, new PropertyChangedEventArgs( propertyName ) );
                }
            }
        }

        /// <summary>
        /// Aspect introducing INPC interface with OnPropertyChanged method
        /// </summary>
        [Internal]
        [AspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
        [MulticastAttributeUsage( MulticastTargets.Class, PersistMetaData = false, AllowMultiple = false, Inheritance = MulticastInheritance.None )]
        public class IntroduceNotifyChildPropertyChangedAttribute : InstanceLevelAspect
        {
            ////TODO build tests 

            [IntroduceMember( OverrideAction = MemberOverrideAction.Fail, Visibility = Visibility.Family )]
            public event EventHandler<ChildPropertyChangedEventArgs> ____PostSharpToolkitsDomain_ChildPropertyChanged____;

            [IntroduceMember( OverrideAction = MemberOverrideAction.Fail, Visibility = Visibility.Family )]
            public void ____PostSharpToolkitsDomain_OnChildPropertyChanged____( string propertyPath )
            {
                EventHandler<ChildPropertyChangedEventArgs> handler = this.____PostSharpToolkitsDomain_ChildPropertyChanged____;
                if ( handler != null )
                {
                    handler( this.Instance, new ChildPropertyChangedEventArgs( propertyPath ) );
                }
            }
        }

        #endregion
    }
}