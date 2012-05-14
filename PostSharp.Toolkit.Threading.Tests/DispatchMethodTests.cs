using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using NUnit.Framework;

using PostSharp.Toolkit.Threading.Dispatch;

using FormsApplication = System.Windows.Forms.Application;
using WpfApplication = System.Windows.Application;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class DispatchMethodTests
    {
        [Test]
        public void WpfTest()
        {
            DispatchMethodWpfObject window = null;

            Thread windowThread = new Thread(() =>
                {
                    window = new DispatchMethodWpfObject();
                    window.Show();
                    Dispatcher.Run();
                });

            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();

            Thread.Sleep(1000); // wait for the window

            window.SetWindowTitle();

            window.Dispatcher.InvokeShutdown();
            
            windowThread.Join();
        }
    }
    
    public class DispatchMethodWpfObject : Window
    {
        [DispatchMethod]
        public void SetWindowTitle()
        {
            this.Title = "new title";
        }
    }
}
