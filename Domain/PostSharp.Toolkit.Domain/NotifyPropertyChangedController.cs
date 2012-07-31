using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PostSharp.Toolkit.Domain
{
    public static class NotifyPropertyChangedController
    {
        public static void RaiseEvents()
        {
            PropertyChangesTracker.RaisePropertyChangedIncludingCurrentObject();
        }

        public static void RaiseEvents(object instance)
        {
            PropertyChangesTracker.RaisePropertyChangedOnlyOnSpecifiedInstance(instance);
        }
    }
}
