using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

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
                    var application = new WpfApplication();
                    application.Run(window);
                });

            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();

            Thread.Sleep(1000); // wait for the window

            window.SetWindowTitle();

            window.Dispatcher.InvokeShutdown();
            
            windowThread.Join();
        }

        [Test]
        public void WpfWinForms()
        {
            DispatchMethodWinFormsObject window = null;

            Thread windowThread = new Thread(() =>
            {
                window = new DispatchMethodWinFormsObject();
                FormsApplication.Run(window);
            });

            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();

            Thread.Sleep(1000); // wait for the window

            window.AddControl();

            window.Invoke((Action)(() => window.Close()));

            windowThread.Join();
        }
    }

    [System.ComponentModel.DesignerCategory("")]
    public class DispatchMethodWinFormsObject : Form
    {
        public void AddControl()
        {
            this.Controls.Add(new Control()); // Adding control deterministicly throws exception when done from not UI thread 
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
