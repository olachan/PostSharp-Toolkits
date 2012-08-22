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

using PostSharp.Aspects;
using PostSharp.Toolkit.Domain.PropertyDependencyAnalisys;
using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.PropertyChangeTracking
{
    [Serializable]
    internal sealed class ChildPropertyChangedProcessor
    {
        // collection of handelers attached to objects. Maintained to unhook handler when no longer needed.
        [NonSerialized]
        private readonly Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor> notifyChildPropertyChangedHandlers;

        // comparer to compare old and new value based on field type (reference, value)
        private readonly FieldValueComparer fieldValueComparer;

        private readonly ExplicitDependencyMap explicitDependencyMap;

        // map connecting property to field if property depends exactly on one field. Moreover return types of property and field match.
        private readonly PropertyFieldBindingsMap propertyToFieldBindings;

        private readonly object instance;

        private ChildPropertyChangedProcessor( ChildPropertyChangedProcessor prototype, object instance )
        {
            this.instance = instance;
            this.fieldValueComparer = prototype.fieldValueComparer;
            this.explicitDependencyMap = prototype.explicitDependencyMap;
            this.propertyToFieldBindings = PropertyFieldBindingsMap.CreateFromPrototype( prototype.propertyToFieldBindings );
            this.notifyChildPropertyChangedHandlers = new Dictionary<string, NotifyChildPropertyChangedEventHandlerDescriptor>();
        }

        private ChildPropertyChangedProcessor(
            PropertyFieldBindingsMap propertyToFieldBindings, FieldValueComparer fieldValueComparer, ExplicitDependencyMap explicitDependencyMap )
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

        public void HandleGetProperty( LocationInterceptionArgs args )
        {
            if ( this.propertyToFieldBindings.RefreshPropertyBindings( args ) )
            {
                this.UnHookNotifyChildPropertyChangedHandler( args );
            }
            else
            {
                this.ReHookNotifyChildPropertyChangedHandler( args );
            }
        }

        // hook handlers to all fields that contain not null value before constructor execution (field initializer assigned values)
        public void HookHandlersToAllFields()
        {
            foreach ( FieldInfoWithCompiledGetter fieldInfo in this.propertyToFieldBindings.FiledInfos.Values )
            {
                this.HookNotifyChildPropertyChangedHandler( fieldInfo.GetValue( this.instance ), fieldInfo.FieldName );
            }
        }

        public void HookPropertyChangedHandler()
        {
            NotifyPropertyChangedAccessor.AddPropertyChangedHandler( this.instance, this.OnPropertyChanged );
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs eventArgs )
        {
            if (!PropertyChangesTracker.AreEventsFiring)
            {
                this.ChildPropertyChanged( new List<string>(){eventArgs.PropertyName} );
                PropertyChangesTracker.RaisePropertyChangedIfNeeded(this.instance);
            }
        }

        public void HandleFieldChange( LocationInterceptionArgs args )
        {
            List<string> propertyList;
            if ( FieldDependenciesMap.FieldDependentProperties.TryGetValue( args.LocationFullName, out propertyList ) )
            {
                PropertyChangesTracker.StoreChangedProperties( this.instance, propertyList );
            }

            if ( propertyList == null )
            {
                propertyList = new List<string>();
            }
            else
            {
                propertyList = propertyList.ToList();
            }

            propertyList.Add( args.LocationName );

            this.ChildPropertyChanged( propertyList );
            this.ReHookNotifyChildPropertyChangedHandler( args );
        }

        private void NotifyChildPropertyChangedEventHandler( string locationName, ChildPropertyChangedEventArgs args )
        {
            this.ChildPropertyChanged( new List<string> { string.Format( "{0}.{1}", locationName, args.Path ) } );
        }

        private void ChildPropertyChanged( List<string> paths )
        {
            PropertyChangesTracker.StoreChangedChildProperties( this.instance, paths );

            HashSet<string> allChangedProperties = new HashSet<string>();

            do
            {
                paths = paths.Except( allChangedProperties ).ToList();
                foreach ( string path in paths )
                {
                    if ( !allChangedProperties.Contains( path ) )
                    {
                        allChangedProperties.Add( path );
                    }
                }

                List<string> changedProperties = paths.SelectMany( this.explicitDependencyMap.GetDependentProperties ).ToList();

                PropertyChangesTracker.StoreChangedProperties( this.instance, changedProperties );

                paths = paths.SelectMany( this.GetAffectedPaths ).Union( changedProperties ).ToList();

                PropertyChangesTracker.StoreChangedChildProperties( this.instance, paths.ToList() );
            }
            while ( paths.Count > 0 );
        }

        // get properties affected by the change described in path
        private IEnumerable<string> GetAffectedPaths( string path )
        {
            int dotIndex = path.IndexOf( '.' );
            string changedField;
            string changedPath = null;

            if ( dotIndex == -1 )
            {
                changedField = path;
            }
            else
            {
                changedField = path.Substring( 0, dotIndex );
                changedPath = path.Substring( dotIndex + 1 );
            }

            return
                this.propertyToFieldBindings.GetDependentPropertiesBindings( changedField ).Select(
                    d => dotIndex == -1 ? d.PropertyName : string.Format( "{0}.{1}", d.PropertyName, changedPath ) );
        }

        private void ReHookNotifyChildPropertyChangedHandler( LocationInterceptionArgs args )
        {
            NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor;

            if ( this.notifyChildPropertyChangedHandlers.TryGetValue( args.LocationName, out handlerDescriptor ) && handlerDescriptor.Reference.IsAlive )
            {
                if ( !this.fieldValueComparer.AreEqual( args.LocationFullName, args.Value, handlerDescriptor.Reference ) )
                {
                    this.UnHookNotifyChildPropertyChangedHandler( handlerDescriptor );
                    this.HookNotifyChildPropertyChangedHandler( args.Value, args.LocationName );
                }
            }
            else
            {
                this.HookNotifyChildPropertyChangedHandler( args.Value, args.LocationName );
            }
        }

        private void UnHookNotifyChildPropertyChangedHandler( LocationInterceptionArgs args )
        {
            NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor;

            if ( this.notifyChildPropertyChangedHandlers.TryGetValue( args.LocationName, out handlerDescriptor ) && handlerDescriptor.Reference.IsAlive )
            {
                this.UnHookNotifyChildPropertyChangedHandler( handlerDescriptor );
            }
        }

        private void UnHookNotifyChildPropertyChangedHandler( NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor )
        {
            object currentValue = handlerDescriptor.Reference.Target;
            if ( currentValue != null )
            {
                NotifyPropertyChangedAccessor.RemoveChildPropertyChangedHandler( currentValue, handlerDescriptor.Handler );
            }
        }

        private void HookNotifyChildPropertyChangedHandler( object instance, string locationName )
        {
            object currentValue = instance as INotifyPropertyChanged;
            if ( currentValue != null )
            {
                string locationNameClosure = locationName;
                NotifyChildPropertyChangedEventHandlerDescriptor handlerDescriptor = new NotifyChildPropertyChangedEventHandlerDescriptor(
                    currentValue, ( _, a ) => this.NotifyChildPropertyChangedEventHandler( locationNameClosure, a ) );
                this.notifyChildPropertyChangedHandlers.AddOrUpdate( locationNameClosure, handlerDescriptor );
                NotifyPropertyChangedAccessor.AddChildPropertyChangedHandler( currentValue, handlerDescriptor.Handler );
            }
        }

        public static ChildPropertyChangedProcessor CompileTimeCreate(
            Type type,
            Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies,
            FieldValueComparer fieldValueComparer,
            ExplicitDependencyMap explicitDependencyMap )
        {
            PropertyFieldBindingsMap propertyToFieldBindings = PropertyToFieldBindingGenerator.GenerateBindings(
                type, methodFieldDependencies, fieldValueComparer );
            return new ChildPropertyChangedProcessor( propertyToFieldBindings, fieldValueComparer, explicitDependencyMap );
        }

        public static ChildPropertyChangedProcessor CreateFromPrototype( ChildPropertyChangedProcessor prototype, object instance )
        {
            return new ChildPropertyChangedProcessor( prototype, instance );
        }

        [Serializable]
        private sealed class NotifyChildPropertyChangedEventHandlerDescriptor
        {
            public NotifyChildPropertyChangedEventHandlerDescriptor( object reference, EventHandler<ChildPropertyChangedEventArgs> handler )
            {
                this.Reference = new WeakReference( reference );
                this.Handler = handler;
            }

            public WeakReference Reference { get; private set; }

            public EventHandler<ChildPropertyChangedEventArgs> Handler { get; private set; }
        }

        private static class PropertyToFieldBindingGenerator
        {
            public static PropertyFieldBindingsMap GenerateBindings(
                Type type, Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies, FieldValueComparer fieldValueComparer )
            {
                // build propertyToFieldBindings
                PropertyFieldBindingsMap propertyToFieldBindings = new PropertyFieldBindingsMap( type, fieldValueComparer );

                PropertyInfo[] allProperties = type.GetProperties( BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly );

                foreach ( PropertyInfo propertyInfo in allProperties )
                {
                    IList<FieldInfo> fieldList;
                    if ( methodFieldDependencies.TryGetValue( propertyInfo.GetGetMethod(), out fieldList ) )
                    {
                        List<FieldInfo> matchingFields = fieldList.Where( f => propertyInfo.PropertyType.IsAssignableFrom( f.FieldType ) ).ToList();
                        propertyToFieldBindings.AddBindings( propertyInfo, matchingFields, type );
                    }
                }
                return propertyToFieldBindings;
            }
        }
    }
}