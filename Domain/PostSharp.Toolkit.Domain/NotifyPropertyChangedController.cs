using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Inteface allowing to manualy raise PropertyChanged events.
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
        /// Raise all events on secific object
        /// </summary>
        /// <param name="instance">object to raise events on</param>
        public static void RaiseEvents(object instance)
        {
            PropertyChangesTracker.RaisePropertyChangedOnlyOnSpecifiedInstance(instance);
        }
    }
}
