using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Provides access to WPF Dispatcher without direct dependency on WindowsBase.dll.
    /// Dispatcher will only be found if WindowsBase.dll is already loaded into the AppDomain by another assembly.
    /// </summary>
    internal static class WpfDispatcherBinding
    {
        //TODO: Do we need to handle any scenario in which WindowsBase is not yet loaded the first time we look for the dispatcher?
        private static Func<Thread, object> dispatcherProvider;
        private static object syncRoot = new object();
        private static bool initialized = false;
        private static Func<object, bool> checkAccessDelegate;
        private static Action<object, Action> invokeDelegate;
        private static Action<object, Action> beginInvokeDelegate;
        private static Type dispatcherType;


        internal class DispatcherWrapper : IDispatcher
        {
            private readonly object dispatcher;

            public DispatcherWrapper(object dispatcher)
            {
                this.dispatcher = dispatcher;
            }

            public bool CheckAccess()
            {
                return WpfDispatcherBinding.CheckAccess(this.dispatcher);
            }

            public void Invoke(IAction action)
            {
                WpfDispatcherBinding.Invoke(this.dispatcher, action.Invoke);
            }

            public void BeginInvoke(IAction action)
            {
                WpfDispatcherBinding.BeginInvoke(this.dispatcher, action.Invoke);
            }
        }


        internal static bool CheckAccess(object dispatcher)
        {
            return checkAccessDelegate(dispatcher);
        }

        internal static void Invoke(object dispatcher, Action action)
        {
            invokeDelegate(dispatcher, action);
        }

        internal static void BeginInvoke(object dispatcher, Action action)
        {
            beginInvokeDelegate(dispatcher, action);
        }

        internal static IDispatcher TryFindWpfDispatcher(Thread thread)
        {
            if (!initialized)
            {
                lock (syncRoot)
                {
                    if (!initialized)
                    {
                        Initialize();
                        initialized = true;
                    }
                }
            }

            if (dispatcherProvider == null)
            {
                return null;
            }

            object dispatcher = dispatcherProvider(Thread.CurrentThread);
            if (dispatcher != null)
            {
                return new DispatcherWrapper(dispatcher);
            }

            return null;
        }

        private static void Initialize()
        {
            Assembly windowsBaseAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(
                a =>
                a.FullName == "WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"); //TODO: Maybe we should ignore version number?


            if (windowsBaseAssembly != null)
            {
                dispatcherType = windowsBaseAssembly.GetType("System.Windows.Threading.Dispatcher");
                ParameterExpression pe = Expression.Parameter(typeof(Thread));
                MethodCallExpression exp = Expression.Call(dispatcherType, "FromThread", new Type[0], pe);
                dispatcherProvider = Expression.Lambda<Func<Thread, object>>(exp, pe).Compile();

                BuildDispatcherCallDelegates();
            }
        }

        private static void BuildDispatcherCallDelegates()
        {
            var objectParameterExpression = Expression.Parameter(typeof(object));
            var castExpression = Expression.Convert(objectParameterExpression, dispatcherType);

            MethodCallExpression checkAccessCall = Expression.Call(castExpression, "CheckAccess", new Type[0]);
            checkAccessDelegate = Expression.Lambda<Func<object, bool>>(checkAccessCall, objectParameterExpression).Compile();

            ParameterExpression actionParameterExpression = Expression.Parameter(typeof(Delegate));
            var paramsExpression = Expression.Constant(new object[0]);

            MethodCallExpression invokeCall = Expression.Call(castExpression, "Invoke", new Type[0], actionParameterExpression, paramsExpression);
            invokeDelegate = Expression.Lambda<Action<object, Action>>(invokeCall, objectParameterExpression, actionParameterExpression).Compile();

            MethodCallExpression beginInvokeCall = Expression.Call(castExpression, "BeginInvoke", new Type[0], actionParameterExpression, paramsExpression);
            beginInvokeDelegate = Expression.Lambda<Action<object, Action>>(beginInvokeCall, objectParameterExpression, actionParameterExpression).Compile();
        }
    }
}