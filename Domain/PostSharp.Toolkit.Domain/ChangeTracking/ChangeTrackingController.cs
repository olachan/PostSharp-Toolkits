#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public static class ChangeTrackingController
    {
        public static RestorePointToken AddRestorePoint(object trackedObject, string name = null)
        {
            return((ObjectTracker)((ITrackedObject)trackedObject).Tracker).AddRestorePoint(name);
        }

        public static void UndoTo(object trackedObject, string name)
        {
            ((ObjectTracker)((ITrackedObject)trackedObject).Tracker).UndoTo(name);
        }

        public static void RedoTo(object trackedObject, string name)
        {
            ((ObjectTracker)((ITrackedObject)trackedObject).Tracker).RedoTo(name);
        }

        public static void UndoTo(object trackedObject, RestorePointToken token)
        {
            ((ObjectTracker)((ITrackedObject)trackedObject).Tracker).UndoTo(token);
        }

        public static void RedoTo(object trackedObject, RestorePointToken token)
        {
            ((ObjectTracker)((ITrackedObject)trackedObject).Tracker).RedoTo(token);
        }

        public static IDisposable StartAtomicOperation(object trackedObject)
        {
            return ((ObjectTracker)((ITrackedObject)trackedObject).Tracker).StartAtomicOperation();
        }
    }
}