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

        public Hammer Hammer { get; set; }

        public string Name { get; set; }

        public ShellViewModel()
        {
            this.HistoryTracker = new HistoryTracker();
            this.HistoryTracker.Track( this );
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

        public void CreateHammer()
        {
            this.Hammer = new Hammer() { Length = _random.Next(29), Weight = _random.Next(113)};
            this.HistoryTracker.Track( this.Hammer );
            ObjectTracker.SetRestorePoint( this.Hammer, "New" );
        }

        public void ResetHammer()
        {
            ObjectTracker.UndoTo( this.Hammer, "New" );
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