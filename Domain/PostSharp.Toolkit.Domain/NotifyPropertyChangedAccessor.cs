#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;

namespace PostSharp.Toolkit.Domain
{
    internal static class NotifyPropertyChangedAccessor
    {
        private class NotifyPropertyChangedTypeAccessors
        {
            private readonly Action<object, string> raisePropertyChangedAction;
            private readonly Action<object, string> raiseChildPropertyChangedAction;
            private readonly Action<object, EventHandler<ChildPropertyChangedEventArgs>> addChildPropertyChangedHandlerAction;
            private readonly Action<object, EventHandler<ChildPropertyChangedEventArgs>> removeChildPropertyChangedHandlerAction;

            public NotifyPropertyChangedTypeAccessors(Type type)
            {
                ParameterExpression objectParameter = Expression.Parameter( typeof(object), "object" );
                ParameterExpression propertyParameter = Expression.Parameter( typeof(string), "property" );

                raisePropertyChangedAction =
                    Expression.Lambda<Action<object, string>>(Expression.Call(Expression.Convert( objectParameter, type),
                                                                                "OnPropertyChanged", null, propertyParameter), objectParameter, propertyParameter)
                        .Compile();

                //(object, propertyPath) => object.PostsharpToolkitsDomain_OnChildPropertyChanged(propertyName)
                raiseChildPropertyChangedAction =
                    Expression.Lambda<Action<object, string>>(Expression.Call(Expression.Convert(objectParameter, type),
                                                                                "PostSharpToolkitsDomain_OnChildPropertyChanged", null,
                                                                                propertyParameter), objectParameter, propertyParameter).Compile();

                
                ParameterExpression handlerParameter = Expression.Parameter( typeof(EventHandler<ChildPropertyChangedEventArgs>), "handler" );

                //(object, handler) => object.PostsharpToolkitsDomain_ChildPropertyChanged += handler 
                this.addChildPropertyChangedHandlerAction =
                    Expression.Lambda<Action<object, EventHandler<ChildPropertyChangedEventArgs>>>(
                            Expression.Call(
                                Expression.Convert(objectParameter, type), "add_PostSharpToolkitsDomain_ChildPropertyChanged", null, handlerParameter),
                             objectParameter, handlerParameter).Compile();

                //(object, handler) => object.PostsharpToolkitsDomain_ChildPropertyChanged -= handler 
                this.removeChildPropertyChangedHandlerAction =
                    Expression.Lambda<Action<object, EventHandler<ChildPropertyChangedEventArgs>>>(
                        Expression.Call(
                                Expression.Convert(objectParameter, type), "remove_PostSharpToolkitsDomain_ChildPropertyChanged", null, handlerParameter),
                        objectParameter, handlerParameter).Compile();
            }

            public void RaisePropertyChanged(object obj, string propertyName)
            {
                raisePropertyChangedAction.Invoke(obj, propertyName);
            }

            public void RaiseChildPropertyChanged(object obj, string propertyPath)
            {
                raiseChildPropertyChangedAction.Invoke(obj, propertyPath);
            }

            public void AddChildPropertyChangedHandler(object obj, EventHandler<ChildPropertyChangedEventArgs> handler)
            {
                this.addChildPropertyChangedHandlerAction.Invoke(obj, handler);
            }

            public void RemoveChildPropertyChangedHandler(object obj, EventHandler<ChildPropertyChangedEventArgs> handler)
            {
                this.removeChildPropertyChangedHandlerAction.Invoke(obj, handler);
            }
        }

        private static readonly ConcurrentDictionary<Type,NotifyPropertyChangedTypeAccessors> accessors =
            new ConcurrentDictionary<Type,NotifyPropertyChangedTypeAccessors>();

        public static void RaisePropertyChanged(object obj, string propertyName)
        {
            accessors.GetOrAdd( obj.GetType(), t => new NotifyPropertyChangedTypeAccessors( t ) ).RaisePropertyChanged( obj, propertyName );
        }

        public static void AddPropertyChangedHandler(object obj, PropertyChangedEventHandler handler)
        {
            ((INotifyPropertyChanged)obj).PropertyChanged += handler;
        }

        public static void RemovePropertyChangedHandler(object obj, PropertyChangedEventHandler handler)
        {
            ((INotifyPropertyChanged)obj).PropertyChanged -= handler;
        }

        public static void RaiseChildPropertyChanged(object obj, string propertyPath)
        {
            accessors.GetOrAdd( obj.GetType(), t => new NotifyPropertyChangedTypeAccessors( t ) ).RaiseChildPropertyChanged(obj, propertyPath);
        }

        public static void AddChildPropertyChangedHandler(object obj, EventHandler<ChildPropertyChangedEventArgs> handler)
        {
            accessors.GetOrAdd( obj.GetType(), t => new NotifyPropertyChangedTypeAccessors( t ) ).AddChildPropertyChangedHandler(obj, handler);
        }

        public static void RemoveChildPropertyChangedHandler(object obj, EventHandler<ChildPropertyChangedEventArgs> handler)
        {
            accessors.GetOrAdd( obj.GetType(), t => new NotifyPropertyChangedTypeAccessors( t ) ).RemoveChildPropertyChangedHandler(obj, handler);
        }
    }
}