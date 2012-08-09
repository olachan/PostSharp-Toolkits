using System;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;

using NUnit.Framework;

using PostSharp.Toolkit.Diagnostics;
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

                        string output = OutputString.ToString();
                        StringAssert.Contains( "An exception occurred:\nSystem.Exception", output );
                    } );

        }

        private static void RunInWpfWindow(Action<DispatchWpfObject> action)
        {
            DispatchWpfObject window = null;

            ManualResetEventSlim ready = new ManualResetEventSlim(false);

            Thread windowThread = new Thread(
                () =>
                {
                    window = new DispatchWpfObject(ready);
                    //window.Show();
                    ready.Set();
                    //Dispatcher.Run();
                });

            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();

            ready.Wait();
            ready.Reset();

            action(window);

            ready.Wait();

            //window.Dispatcher.InvokeShutdown();

            windowThread.Join();
        }
    }

    public class SimpleClass2
    {
        public string Field1;

        public string Property1 { get; set; }

        public void Method1()
        {
        }

        public void MethodThrowsException()
        {
            throw new Exception("This is an exception");
        }

        public void MethodWith1Argument(string stringArg)
        {
        }

        public void MethodWithObjectArguments(object arg0, StringBuilder arg1)
        {
        }
    }

    //public class DispatchWpfObject : Window
    //{
    //    private ManualResetEventSlim ready;

    //    public DispatchWpfObject(ManualResetEventSlim ready)
    //    {
    //        this.ready = ready;

    //        // Attributes set so window does not show during tests
    //        this.WindowStyle = WindowStyle.None;
    //        this.ShowInTaskbar = false;
    //        this.AllowsTransparency = true;
    //        this.Background = Brushes.Transparent;
    //        this.Width = 0;
    //        this.Height = 0;
    //    }

    //    [DispatchedMethod]
    //    public void SetWindowTitle(bool setEvent = true)
    //    {
    //        this.Title = "new title";
    //        if (setEvent)
    //        {
    //            this.ready.Set();
    //        }
    //    }

    //    [DispatchedMethod]
    //    public void ThrowException()
    //    {
    //        this.ready.Set();
    //        throw new Exception("test exception");
    //    }

    //    [DispatchedMethod(true)]
    //    public void ThrowExceptionAsync()
    //    {
    //        this.ready.Set();
    //        throw new ArgumentException("test exception");
    //    }

    //    protected override void OnInitialized(EventArgs e)
    //    {
    //        this.ready.Set();
    //    }
    //}

    
}
