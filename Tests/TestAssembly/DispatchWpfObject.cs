using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;

using PostSharp.Toolkit.Threading;

namespace TestAssembly
{
    //public class DispatchWpfObject : Window
    //{
    //    private ManualResetEventSlim ready;

    //    public DispatchWpfObject(ManualResetEventSlim ready)
    //    {
    //        this.ready = ready;

    //         // Attributes set so window does not show during tests
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
    //        //this.Title = "new title";
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
