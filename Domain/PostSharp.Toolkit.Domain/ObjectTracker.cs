﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain
{
    public static class ObjectTracker
    {
        public static RestorePointToken SetRestorePoint( object trackedObject, string name = null )
        {
            ITrackedObject to = CheckObject( trackedObject );
            return ((AggregateTracker)to.Tracker).AddRestorePoint( name );
        }

        public static bool RestorePointExists(object trackedObject, string restorePoint)
        {
            ITrackedObject to = CheckObject(trackedObject);
            return ((AggregateTracker)to.Tracker).RestorePointExists(restorePoint);
        }

        public static bool RestorePointExists(object trackedObject, RestorePointToken restorePoint)
        {
            ITrackedObject to = CheckObject(trackedObject);
            return ((AggregateTracker)to.Tracker).RestorePointExists(restorePoint);
        }

        public static void UndoTo( object trackedObject, string name )
        {
            ITrackedObject to = CheckObject( trackedObject );
            ((AggregateTracker)to.Tracker).UndoTo( name );
        }

        public static void RedoTo( object trackedObject, string name )
        {
            ITrackedObject to = CheckObject( trackedObject );
            ((AggregateTracker)to.Tracker).RedoTo( name );
        }

        public static void UndoTo( object trackedObject, RestorePointToken token )
        {
            ITrackedObject to = CheckObject( trackedObject );
            ((AggregateTracker)to.Tracker).UndoTo( token );
        }

        public static void RedoTo( object trackedObject, RestorePointToken token )
        {
            ITrackedObject to = CheckObject( trackedObject );
            ((AggregateTracker)to.Tracker).RedoTo( token );
        }

        public static IDisposable StartAtomicOperation( object trackedObject, string name )
        {
            ITrackedObject to = CheckObject( trackedObject );
            return ((AggregateTracker)to.Tracker).StartAtomicOperation(name);
        }

        public static object GetAggregateRoot( object trackedObject )
        {
            ITrackedObject to = CheckObject( trackedObject );
            return ((AggregateTracker)to.Tracker).AggregateRoot;
        }

        public static void Track( object trackedObject )
        {
            ITrackedObject to = CheckObject( trackedObject );
            ((AggregateTracker)to.Tracker).Track();
        }

        public static void StopTracking( object trackedObject )
        {
            ITrackedObject to = CheckObject( trackedObject );
            ((AggregateTracker)to.Tracker).StopTracking();
        }

        public static bool CanStopTracking(object trackedObject)
        {
            ITrackedObject to = CheckObject(trackedObject);
            return ((AggregateTracker)to.Tracker).CanStopTracking();
        }

        internal static ITrackedObject CheckObject( object trackedObject )
        {
            ITrackedObject to;
            if ( (to = trackedObject as ITrackedObject) == null )
            {
                throw new ArgumentException( "Passed object is not instrumented by TrackedObject attribute" );
            }

            if ( !to.IsAggregateRoot )
            {
                throw new ArgumentException( "Passed object is not aggregate root" );
            }

            return to;
        }
    }
}