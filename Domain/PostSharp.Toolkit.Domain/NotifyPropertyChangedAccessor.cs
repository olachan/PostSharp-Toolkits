#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

using System.Linq;

namespace PostSharp.Toolkit.Domain
{
    internal static class NotifyPropertyChangedAccessor
    {
        internal class NotifyPropertyChangedTypeAccessors
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
                                                                                "____PostSharpToolkitsDomain_OnChildPropertyChanged____", null,
                                                                                propertyParameter), objectParameter, propertyParameter).Compile();

                
                ParameterExpression handlerParameter = Expression.Parameter( typeof(EventHandler<ChildPropertyChangedEventArgs>), "handler" );

                //(object, handler) => object.PostsharpToolkitsDomain_ChildPropertyChanged += handler 
                this.addChildPropertyChangedHandlerAction =
                    Expression.Lambda<Action<object, EventHandler<ChildPropertyChangedEventArgs>>>(
                            Expression.Call(
                                Expression.Convert(objectParameter, type), "add_____PostSharpToolkitsDomain_ChildPropertyChanged____", null, handlerParameter),
                             objectParameter, handlerParameter).Compile();

                //(object, handler) => object.PostsharpToolkitsDomain_ChildPropertyChanged -= handler 
                this.removeChildPropertyChangedHandlerAction =
                    Expression.Lambda<Action<object, EventHandler<ChildPropertyChangedEventArgs>>>(
                        Expression.Call(
                                Expression.Convert(objectParameter, type), "remove_____PostSharpToolkitsDomain_ChildPropertyChanged____", null, handlerParameter),
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

        private static ConcurrentDictionary<Type,NotifyPropertyChangedTypeAccessors> accessors =
            new ConcurrentDictionary<Type,NotifyPropertyChangedTypeAccessors>();

        //internal static Dictionary<Type,NotifyPropertyChangedTypeAccessors> GetForSerialization()
        //{
        //    return accessors.ToDictionary( kv => kv.Key, kv => kv.Value );
        //}

        //internal static void SetAfterDeserialization(Dictionary<Type,NotifyPropertyChangedTypeAccessors> deserializedDictionary)
        //{
        //    accessors = new ConcurrentDictionary<Type, NotifyPropertyChangedTypeAccessors>(deserializedDictionary);
        //}

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