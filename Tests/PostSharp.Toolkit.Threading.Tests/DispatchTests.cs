#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using NUnit.Framework;
using PostSharp.Toolkit.Threading.Dispatching;
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

            ManualResetEventSlim ready = new ManualResetEventSlim( false );
            Thread windowThread = new Thread( () =>
                                                  {
                                                      window = new DispatchWpfObject( ready );
                                                      window.Show();
                                                      Dispatcher.Run();
                                                  } );

            windowThread.SetApartmentState( ApartmentState.STA );
            windowThread.Start();

            ready.Wait();

            window.SetWindowTitle();

            window.Dispatcher.InvokeShutdown();

            windowThread.Join();
        }

        private DispatchWinFormsObject form;

        [Test]
        public void WinFormsMethods_AreDispatchedCorrectly()
        {
            form = null;
            ManualResetEventSlim ready = new ManualResetEventSlim( false );
            Thread windowThread = new Thread( () =>
                                                  {
                                                      form = new DispatchWinFormsObject( ready );
                                                      FormsApplication.Run( form );
                                                  } );

            windowThread.SetApartmentState( ApartmentState.STA );
            windowThread.Start();

            ready.Wait();

            form.AddControl();

            form.Invoke( new Action( form.Close ) );

            windowThread.Join();
        }

        [DesignerCategory( "" )]
        public class DispatchWinFormsObject : Form
        {
            private readonly ManualResetEventSlim readyEvent;

            public DispatchWinFormsObject( ManualResetEventSlim ready )
            {
                this.readyEvent = ready;
            }

            [DispatchedMethod]
            public void AddControl()
            {
                this.Controls.Add( new Control() ); // Adding control deterministically throws exception when done from outside UI thread 
            }


            protected override void OnShown( EventArgs e )
            {
                this.readyEvent.Set();
            }
        }

        public class DispatchWpfObject : Window
        {
            private ManualResetEventSlim ready;

            public DispatchWpfObject( ManualResetEventSlim ready )
            {
                this.ready = ready;
                Debug.Write( "Test" );
            }

            [DispatchedMethod]
            public void SetWindowTitle()
            {
                this.Title = "new title";
            }

            protected override void OnInitialized( EventArgs e )
            {
                this.ready.Set();
            }
        }
    }
}