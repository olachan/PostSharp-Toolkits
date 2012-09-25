using System;
using System.Windows;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.TestApp
{
    [TrackedObject] //TODO: Option to exclude all properties by default (opt-in behavior)
    public class ShellViewModel : ViewModelBase
    {
        [ChangeTrackingIgnoreField]
        public HistoryTracker HistoryTracker { get; protected set; }

        public Toolbox Toolbox { get; set; }

        [OperationName("NameChanged")]
        public string Name { get; set; }

        public ShellViewModel()
        {
            this.HistoryTracker = new HistoryTracker();
            this.HistoryTracker.Track( this );
            this.CreateToolbox();
        }

        public bool CanSayHello
        {
            get { return !string.IsNullOrWhiteSpace(Name); }
        }

        public void SayHello()
        {
            MessageBox.Show(string.Format("Hello {0}!", Name));
        }

        private static readonly Random _random = new Random();

        public void CreateToolbox()
        {
            this.Toolbox = new Toolbox();
            this.HistoryTracker.Track( this.Toolbox );
            //this.Toolbox.CreateNewHammer();

            ObjectTracker.SetRestorePoint( this.Toolbox, "New" );
            NotifyPropertyChangedController.RaisePropertyChanged( this, vm => vm.CanRevert );
        }

        public void Revert()
        {
            if (this.Toolbox != null)
            {
                ObjectTracker.UndoTo( this.Toolbox, "New" );
            }
        }

        [NotifyPropertyChangedSafe]
        public bool CanRevert
        {
            get
            {
                return this.Toolbox != null && ObjectTracker.RestorePointExists( this.Toolbox, "New" );
            }
        }
    }
}