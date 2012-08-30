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
        public static void Undo(ITrackedObject trackedObject)
        {
            ((ObjectTracker)trackedObject.Tracker).Undo();
        }

        public static void Redo(ITrackedObject trackedObject)
        {
            ((ObjectTracker)trackedObject.Tracker).Redo();
        }

        public static void AddRestorePoint(ITrackedObject trackedObject, string name)
        {
            ((ObjectTracker)trackedObject.Tracker).AddNamedRestorePoint(name);
        }

        public static void UndoToRestorePoint(ITrackedObject trackedObject, string name)
        {
            ((ObjectTracker)trackedObject.Tracker).RestoreNamedRestorePoint(name);
        }

        public static IDisposable StartAtomicOperation(ITrackedObject trackedObject)
        {
            return ((ObjectTracker)trackedObject.Tracker).StartAtomicOperation();
        }
    }
}