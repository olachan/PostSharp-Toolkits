using System.Linq;
using System.Windows.Input;

namespace PostSharp.Toolkit.Domain.Controls
{
    public class UndoButton : UndoRedoButtonBase
    {
        public UndoButton()
        {
            this.DefaultStyleKey = typeof(UndoButton);
        }

        protected override void SetOperations()
        {
            this.Operations = this.HistoryTracker.UndoOperations.Reverse().Take(this.MaxOperationsCount);
            this.OnPropertyChanged("Operations");
        }

        public override ICommand RevertOperationCommand
        {
            get
            {
                return this.HistoryTracker != null ? new UndoRedoCommand(this, true) : null;
            }
        }

        protected override void OnClickCore()
        {
            if (this.HistoryTracker != null && this.HistoryTracker.CanUndo())
            {
                this.HistoryTracker.Undo();
            }
        }
    }
}