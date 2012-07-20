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
            PropertyChangesTracker.RaisePropertyChanged();
        }

        public static void RaiseEvents(object instance)
        {
            PropertyChangesTracker.RaisePropertyChanged(instance);
        }
    }
}
