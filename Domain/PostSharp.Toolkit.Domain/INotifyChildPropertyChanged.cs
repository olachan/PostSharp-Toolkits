#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

namespace PostSharp.Toolkit.Domain
{
    public interface INotifyChildPropertyChanged
    {
        void RaisePropertyChanged(string propertyName);
        void RaiseChildPropertyChanged( NotifyChildPropertyChangedEventArgs args );

        event EventHandler<NotifyChildPropertyChangedEventArgs> ChildPropertyChanged;
    }
}