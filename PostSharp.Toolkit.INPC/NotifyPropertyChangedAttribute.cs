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
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.INPC
{
    /// <summary>
    /// Under development. Early version !!!
    /// </summary>
    [Serializable]
    [MulticastAttributeUsage( MulticastTargets.Class, Inheritance = MulticastInheritance.Strict )]
    [IntroduceInterface( typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore )]
    public class NotifyPropertyChangedAttribute : InstanceLevelAspect, INotifyPropertyChanged
    {
        // Compile-time use only
        [NonSerialized]
        private static Lazy<PropertiesDependencieAnalyzer> analyzer = new Lazy<PropertiesDependencieAnalyzer>();

        // Used for serializing propertyDependencyMap
        private PropertyDependencySerializationStore propertyDependencySerializationStore;

        [OnSerializing]
        public void OnSerializing( StreamingContext context )
        {
            //TODO: Consider better serialization mechanism - maybe simply introduce assembly level aspect doing the stuff below?

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
        [MulticastPointcut( Targets = MulticastTargets.Field )]
        public void OnFieldSet( LocationInterceptionArgs args )
        {
            args.ProceedSetValue();
            IList<string> propertyList;
            if ( FieldDependenciesMap.FieldDependentProperties.TryGetValue( args.LocationFullName, out propertyList ) )
            {
                foreach ( string propertyName in propertyList )
                {
                    PropertyChangesTracker.Accumulator.AddProperty( args.Instance, propertyName );
                }
            }

            PropertyChangesTracker.RaisePropertyChanged( args.Instance, this.OnPropertyChangedMethod,  false );
        }

        [OnMethodInvokeAdvice]
        [MethodPointcut( "SelectMethods" )]
        public void OnMethodInvoke( MethodInterceptionArgs args )
        {
            try
            {
                PropertyChangesTracker.StackContext.PushOnStack( args.Instance );
                args.Proceed();
            }
            finally
            {
                PropertyChangesTracker.RaisePropertyChanged( args.Instance, this.OnPropertyChangedMethod, true );
            }
        }

        private IEnumerable<MethodBase> SelectMethods( Type type )
        {
            return
                type.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ).Where(
                    m => !m.GetCustomAttributes( typeof(NoAutomaticPropertyChangedNotificationsAttribute), true ).Any() );
        }

        public override void CompileTimeInitialize( Type type, AspectInfo aspectInfo )
        {
            analyzer.Value.AnalyzeType( type );
        }

        [ImportMember("OnPropertyChanged")]
        public Action<string> OnPropertyChangedMethod;

        [IntroduceMember( Visibility = Visibility.Family, IsVirtual = true, OverrideAction = MemberOverrideAction.Ignore )]
        public void OnPropertyChanged( string propertyName )
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if ( handler != null )
            {
                handler( this.Instance, new PropertyChangedEventArgs( propertyName ) );
            }
        }

        [IntroduceMember( OverrideAction = MemberOverrideAction.Ignore )]
        public event PropertyChangedEventHandler PropertyChanged;
    }

    //TODO: Rename!
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Method )]
    public class NoAutomaticPropertyChangedNotificationsAttribute : Attribute
    {
    }

    [AttributeUsage( AttributeTargets.Method )]
    public class StateIndependentMethod : Attribute
    {
    }
}