#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.ComponentModel;
using System.Linq.Expressions;

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
        public static void RaiseEvents(object instance)
        {
            PropertyChangesTracker.RaisePropertyChangedOnlyOnSpecifiedInstance(instance);
        }

        public static void RaisePropertyChanged(object instance, string propertyName)
        {
            NotifyPropertyChangedAccessor.RaisePropertyChanged(instance, propertyName);
        }

        public static void RaisePropertyChanged<T, TProperty>(T instance, Expression<Func<T, TProperty>> propertySelector)
        {
            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = propertySelector.Body as UnaryExpression;
                if (unaryExpression != null)
                {
                    memberExpression = unaryExpression.Operand as MemberExpression;
                    if (memberExpression == null)
                        throw new NotSupportedException();
                }
                else
                    throw new NotSupportedException();
            }

            RaisePropertyChanged(instance, memberExpression.Member.Name);
        }
    }
}