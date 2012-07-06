#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PostSharp.Toolkit.INPC
{
    internal static class PropertyChangesTracker
    {
        private static readonly ThreadLocal<ChangedPropertiesAccumulator> changedPropertiesAcumulator =
            new ThreadLocal<ChangedPropertiesAccumulator>( () => new ChangedPropertiesAccumulator() );

        private static readonly ThreadLocal<StackContext> stackTrace = new ThreadLocal<StackContext>( () => new StackContext() );

        public static ChangedPropertiesAccumulator Accumulator
        {
            get
            {
                return changedPropertiesAcumulator.Value;
            }
        }

        public static StackContext StackContext
        {
            get
            {
                return stackTrace.Value;
            }
        }

        public static void RaisePropertyChanged( object instance, bool popFromStack )
        {
            ChangedPropertiesAccumulator accumulator = changedPropertiesAcumulator.Value;
            if ( popFromStack )
            {
                stackTrace.Value.Pop();
            }

            if ( stackTrace.Value.Count > 0 && stackTrace.Value.Peek() == instance )
            {
                return;
            }

            accumulator.Compact();

            List<WeakPropertyDescriptor> objectsToRaisePropertyChanged =
                accumulator.Where( w => w.Instance.IsAlive && !stackTrace.Value.Contains( w.Instance.Target ) ).ToList();

            foreach ( WeakPropertyDescriptor w in objectsToRaisePropertyChanged )
            {
                accumulator.Remove( w );

                IRaiseNotifyPropertyChanged rpc = w.Instance.Target as IRaiseNotifyPropertyChanged;
                if ( rpc != null )
                {
                    rpc.OnPropertyChanged( w.PropertyName );
                }
            }
        }
    }
}