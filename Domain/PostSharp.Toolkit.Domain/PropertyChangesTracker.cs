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

namespace PostSharp.Toolkit.Domain
{
    internal static class PropertyChangesTracker
    {
        private static readonly ThreadLocal<ChangedPropertiesAccumulator> changedPropertiesAcumulator =
            new ThreadLocal<ChangedPropertiesAccumulator>( () => new ChangedPropertiesAccumulator() );

        private static readonly ThreadLocal<StackContext> stackTrace = new ThreadLocal<StackContext>( () => new StackContext() );

        private static ChangedPropertiesAccumulator Accumulator
        {
            get
            {
                return changedPropertiesAcumulator.Value;
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

        public static void HandleFieldChange(object instance, string locationFullName)
        {
            IList<string> propertyList;
            if (FieldDependenciesMap.FieldDependentProperties.TryGetValue(locationFullName, out propertyList))
            {
                StoreChangedProperties(instance, propertyList);
            }
        }

        public static void StoreChangedProperties(object instance, IEnumerable<string> properties)
        {
            Accumulator.AddProperties(instance, properties);
        }

        public static void RaisePropertyChanged()
        {
            ChangedPropertiesAccumulator accumulator = changedPropertiesAcumulator.Value;
            
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

                if (cpc != null) //Target may not be alive any more
                {
                    cpc.RaisePropertyChanged( w.PropertyName );
                    cpc.RaiseChildPropertyChanged( new NotifyChildPropertyChangedEventArgs( w.PropertyName ) );
                }
            }
        }
    }
}