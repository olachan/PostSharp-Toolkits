using System;
using System.Windows;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.TestApp
{
    [TrackedObject] //TODO: Option to exclude all properties by default (opt-in behavior)
    public class ShellViewModel : ViewModelBase
    {
        //TODO: Need a way to exclude field/property from change tracking
        public HistoryTracker HistoryTracker { get; protected set; }

        public Toolbox Toolbox { get; set; }

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
        }

        public void Revert()
        {
            ObjectTracker.UndoTo( this.Toolbox, "New" );
        }

        public bool CanResetHammder()
        {
            //TODO: Need API
            return true;
        }

        public void Undo()
        {
            HistoryTracker.Undo(  ); //TODO: addToParent argument does not make sense here...
        }

        public bool CanUndo()
        {
            //TODO: Need API
            return true;
        }

        public void Redo()
        {
            HistoryTracker.Redo( ); //TODO: addToParent argument does not make sense here...
        }

        public bool CanRedo()
        {
            //TODO: Need API
            return true;
        }
    }
}