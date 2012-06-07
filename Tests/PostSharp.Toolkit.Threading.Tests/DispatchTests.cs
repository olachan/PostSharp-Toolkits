#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using NUnit.Framework;
using FormsApplication = System.Windows.Forms.Application;

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
            Thread windowThread = new Thread(
                () =>
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
            this.form = null;
            ManualResetEventSlim ready = new ManualResetEventSlim( false );
            Thread windowThread = new Thread( () =>
                                                  {
                                                      this.form = new DispatchWinFormsObject( ready );
                                                      FormsApplication.Run( this.form );
                                                  } );

            windowThread.SetApartmentState( ApartmentState.STA );
            windowThread.Start();

            ready.Wait();

            this.form.AddControl();

            this.form.Invoke( new Action( this.form.Close ) );

            windowThread.Join();
        }

        [DesignerCategory( "" )]
        public class DispatchWinFormsObject : Form
        {
            private readonly ManualResetEventSlim readyEvent;

            public DispatchWinFormsObject( ManualResetEventSlim ready )
            {
                this.readyEvent = ready;

                // Attributes set so window does not show during tests
                this.FormBorderStyle = FormBorderStyle.None;
                this.Width = 0;
                this.Height = 0;
                this.ShowInTaskbar = false;
                this.SetStyle( ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true );
            }

            // overriden so window does not show during tests
            protected override void OnPaint( PaintEventArgs e )
            {
            }

            // overriden so window does not show during tests
            protected override void OnPaintBackground( PaintEventArgs e )
            {
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

                // Attributes set so window does not show during tests
                this.WindowStyle = WindowStyle.None;
                this.ShowInTaskbar = false;
                this.AllowsTransparency = true;
                this.Background = Brushes.Transparent;
                this.Width = 0;
                this.Height = 0;
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