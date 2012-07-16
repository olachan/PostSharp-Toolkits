#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Domain
{
    internal static class PropertyChangesTracker
    {
        private static readonly ThreadLocal<ChangedPropertiesAccumulator> propertyChangesAcumulator =
            new ThreadLocal<ChangedPropertiesAccumulator>( () => new ChangedPropertiesAccumulator() );
        private static readonly ThreadLocal<ChangedPropertiesAccumulator> childPropertyChangesAcumulator =
            new ThreadLocal<ChangedPropertiesAccumulator>(() => new ChangedPropertiesAccumulator());

        private static readonly ThreadLocal<StackContext> stackTrace = new ThreadLocal<StackContext>( () => new StackContext() );

        private static ChangedPropertiesAccumulator PropertyChangesAccumulator
        {
            get
            {
                return propertyChangesAcumulator.Value;
            }
        }

        private static ChangedPropertiesAccumulator ChildPropertyChangesAccumulator
        {
            get
            {
                return childPropertyChangesAcumulator.Value;
            }
        }

        private static StackContext StackContext
        {
            get
            {
                return stackTrace.Value;
            }
        }

        public static void PushOnStack( object o )
        {
            StackContext.PushOnStack( o );
        }

        public static object PopFromStack()
        {
            return StackContext.Pop();
        }

        public static object StackPeek()
        {
            return StackContext.Count == 0 ? null : StackContext.Peek();
        }

        public static void HandleFieldChange( LocationInterceptionArgs args)
        {
            List<string> propertyList;
            if (FieldDependenciesMap.FieldDependentProperties.TryGetValue(args.LocationFullName, out propertyList))
            {
                StoreChangedProperties(args.Instance, propertyList);
            }
            StoreChangedChildProperty(args.Instance, args.LocationName);

            if (StackPeek() != args.Instance)
            {
                RaisePropertyChanged();
            }
        }

        public static void StoreChangedProperties(object instance, List<string> properties)
        {
            StoreChangedChildProperties( instance, properties );
            PropertyChangesAccumulator.AddProperties(instance, properties);
        }

        public static void StoreChangedChildProperty(object instance, string propertyPath)
        {
            ChildPropertyChangesAccumulator.AddProperty( instance, propertyPath );
        }

        public static void StoreChangedChildProperties(object instance, List<string> propertyPaths)
        {
            ChildPropertyChangesAccumulator.AddProperties(instance, propertyPaths);
        }

        public static void RaisePropertyChanged()
        {
            RaisePropertyChangesInternal( childPropertyChangesAcumulator.Value, true );
            RaisePropertyChangesInternal(propertyChangesAcumulator.Value, false);
        }

        private static void RaisePropertyChangesInternal( ChangedPropertiesAccumulator accumulator, bool raiseChildPropertyChanges )
        {
            accumulator.Compact();

            List<WeakPropertyDescriptor> objectsToRaisePropertyChanged =
                accumulator.Where( w => w.Instance.IsAlive && !stackTrace.Value.Contains( w.Instance.Target ) ).ToList();

            foreach ( WeakPropertyDescriptor w in objectsToRaisePropertyChanged )
            {
                //INPC handler may raise INPC again and process some of our events;
                //we're working on accumulator copy, so only way to know it is to have a flag on the descriptor
                if ( w.Processed )
                {
                    continue;
                }

                w.Processed = true;
                accumulator.Remove( w );
                INotifyChildPropertyChanged cpc = w.Instance.Target as INotifyChildPropertyChanged;

                if ( cpc != null ) //Target may not be alive any more
                {
                    if (raiseChildPropertyChanges)
                    {
                        cpc.RaiseChildPropertyChanged(new NotifyChildPropertyChangedEventArgs(w.PropertyPath));
                    }
                    else
                    {
                        cpc.RaisePropertyChanged( w.PropertyPath );
                    }
                }
            }
        }
    }
}