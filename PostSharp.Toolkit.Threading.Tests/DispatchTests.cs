using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

using NUnit.Framework;

using PostSharp.Toolkit.Threading.Dispatch;
using PostSharp.Toolkit.Threading.ThreadAffined;
using FormsApplication = System.Windows.Forms.Application;
using WpfApplication = System.Windows.Application;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class DispatchTests
    {
        [Test]
        public void WpfWindowMethods_AreDispatchedCorrectly()
        {
            DispatchWpfObject window = null;

            Thread windowThread = new Thread(() =>
                {
                    window = new DispatchWpfObject();
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
        public void WinFormsMethods_AreDispatchedCorrectly()
        {
            DispatchWinFormsObject window = null;

            Thread windowThread = new Thread(() =>
            {
                window = new DispatchWinFormsObject();
                FormsApplication.Run(window);
            });

            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();

            Thread.Sleep(1000); // wait for the window

            window.AddControl();

            window.Close();

            windowThread.Join();
        }

        [System.ComponentModel.DesignerCategory("")]
        [ThreadAffined]
        public class DispatchWinFormsObject : Form
        {
            public DispatchWinFormsObject()
            {
                Debug.Write("Test");
            }

            [Dispatch]
            public void AddControl()
            {
                this.Controls.Add(new Control()); // Adding control deterministically throws exception when done from outside UI thread 
            }
        }

        [ThreadAffined]
        public class DispatchWpfObject : Window
        {
            public DispatchWpfObject()
            {
                Debug.Write("Test");
            }

            [Dispatch]
            public void SetWindowTitle()
            {
                this.Title = "new title";
            }
        }
    }

   
}
