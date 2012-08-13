using System;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using NUnit.Framework;

using PostSharp.Toolkit.Diagnostics;
using PostSharp.Toolkit.Domain;
using PostSharp.Toolkit.Threading;

using TestAssembly;

//using TestAssembly;

namespace PostSharp.Toolkit.Integration.Tests
{
    [TestFixture]
    public class DignosticThreadingIntegrationTests : BaseTestsFixture
    {
        [Test]
        public void LoggingToolkit_OnException_PrintsException()
        {
            RunInWpfWindow(
                window => 
                    {
                        try
                        {
                            window.ThrowException();
                        }
                        catch (Exception e)
                        {
                        }

                        
                    } );

            string output = OutputString.ToString();
            StringAssert.Contains("An exception occurred:\nSystem.AggregateException", output);
        }

        [Test]
        public void LoggingToolkit_Methods_LogsMethodEnter()
        {
            SimpleClass s = new SimpleClass();
            s.MethodWith1Argument("asd");

            string output = OutputString.ToString();
            StringAssert.Contains("Entering: PostSharp.Toolkit.Integration.Tests.SimpleClass.MethodWith1Argument(string stringArg = \"asd\")", output);
        }

        [Test]
        public void SimpleClass_WhenExceptionThrownFromBackgroundMethod_LoggignDoesNotPrintException()
        {
            SimpleClass s = new SimpleClass();
            try
            {
                s.MethodThrowsException();
            }
            catch (Exception e)
            {
            }
             

            string output = OutputString.ToString();

            Thread.Sleep( 500 );

            GC.Collect();

            StringAssert.DoesNotContain("An exception occurred:", output);
        }

        private static void RunInWpfWindow(Action<DispatchWpfObject2> action)
        {
            DispatchWpfObject2 window = null;

            ManualResetEventSlim ready = new ManualResetEventSlim(false);

            Thread windowThread = new Thread(
                () =>
                {
                    window = new DispatchWpfObject2(ready);
                    window.Show();
                    ready.Set();
                    Dispatcher.Run();
                });

            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();

            ready.Wait();
            ready.Reset();

            action(window);

            ready.Wait();

            window.Dispatcher.InvokeShutdown();

            windowThread.Join();
        }
    }

    [NotifyPropertyChanged]
    public class SimpleClass
    {
        public string Field1;

        public string Property1 { get; set; }

        public void Method1()
        {
        }

        [BackgroundMethod]
        public void MethodThrowsException()
        {
            throw new Exception("This is an exception");
        }

        [BackgroundMethod]
        public void MethodWith1Argument(string stringArg)
        {
        }

        public void MethodWithObjectArguments(object arg0, StringBuilder arg1)
        {
        }
    }

    [NotifyPropertyChanged]
    public class SimpleDispatcherObject
    {
        public string Field1;

        public string Property1 { get; set; }

        public void Method1()
        {
        }

        // [BackgroundMethod]
        public void MethodThrowsException()
        {
            throw new Exception("This is an exception");
        }

        [DispatchedMethod]
        public void MethodWith1Argument(string stringArg)
        {
        }

        public void MethodWithObjectArguments(object arg0, StringBuilder arg1)
        {
        }

        // public IDispatcher Dispatcher { get; private set; }
    }

    public class DispatchWpfObject2 : Window
    {
        private ManualResetEventSlim ready;

        public DispatchWpfObject2(ManualResetEventSlim ready)
        {
            this.ready = ready;

            // Attributes set so window does not show during tests
            this.WindowStyle = WindowStyle.None;
            this.ShowInTaskbar = false;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Width = 0;
            this.Height = 0;
        }

        [DispatchedMethod]
        public void SetWindowTitle(bool setEvent = true)
        {
            this.Title = "new title";
            if (setEvent)
            {
                this.ready.Set();
            }
        }

        [DispatchedMethod]
        public void ThrowException()
        {
            this.ready.Set();
            throw new Exception("test exception");
        }

        [DispatchedMethod(true)]
        public void ThrowExceptionAsync()
        {
            this.ready.Set();
            throw new ArgumentException("test exception");
        }

        protected override void OnInitialized(EventArgs e)
        {
            this.ready.Set();
        }
    }

    
}
