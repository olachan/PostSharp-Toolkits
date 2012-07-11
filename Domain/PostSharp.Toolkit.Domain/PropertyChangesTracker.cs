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
    public static class PropertyChangesTracker
    {
        private static readonly ThreadLocal<ChangedPropertiesAccumulator> changedPropertiesAcumulator =
            new ThreadLocal<ChangedPropertiesAccumulator>( () => new ChangedPropertiesAccumulator() );

        //private static ThreadLocal<bool> propertyChangeRoutineRunning = new ThreadLocal<bool>( () => false );

        private static readonly ThreadLocal<StackContext> stackTrace = new ThreadLocal<StackContext>( () => new StackContext() );

        internal static ChangedPropertiesAccumulator Accumulator
        {
            get
            {
                return changedPropertiesAcumulator.Value;
            }
        }

        internal static StackContext StackContext
        {
            get
            {
                return stackTrace.Value;
            }
        }

        public static void RaisePropertyChanged( object instance, Action<string> onPropertyChanged, bool popFromStack )
        {
            //propertyChangeRoutineRunning.Value = true;

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
                if ( w.Processed )
                {
                    continue;
                }

                w.Processed = true;
                accumulator.Remove( w );

                
                if ( onPropertyChanged != null )
                {
                    onPropertyChanged( w.PropertyName );
                }

                IPropagatedChange pc = w.Instance.Target as IPropagatedChange;
                if ( pc != null )
                {
                    pc.RaisePropagatedChange( new PropagatedChangeEventArgs( w.PropertyName ) );
                }
            }
        }
    }
}