#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Toolkit.Domain.PropertyChangeTracking;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Interface allowing to manually raise PropertyChanged events.
    /// </summary>
    public static class NotifyPropertyChangedController
    {
        /// <summary>
        /// Raise all events on objects not in call stack
        /// </summary>
        public static void RaiseEvents()
        {
            PropertyChangesTracker.RaisePropertyChangedIncludingCurrentObject();
        }

        /// <summary>
        /// Raise all events on specific object
        /// </summary>
        /// <param name="instance">object to raise events on</param>
        public static void RaiseEvents( object instance )
        {
            PropertyChangesTracker.RaisePropertyChangedOnlyOnSpecifiedInstance( instance );
        }
    }
}