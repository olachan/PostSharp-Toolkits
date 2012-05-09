using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using NUnit.Framework;

using PostSharp.Toolkit.Threading.Dispatch;

using Application = System.Windows.Forms.Application;

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
                    window.ShowDialog();
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
                Application.Run(window);
                // window.ShowDialog();
            });

            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();

            Thread.Sleep(1000); // wait for the window

            window.AddTextBox();

            window.Close();

            windowThread.Join();
        }
    }

    public class DispatchMethodWinFormsObject : Form
    {
        public void AddTextBox()
        {
            this.Controls.Add(new TextBox());
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
