using System.Linq;
using System.Windows.Input;

namespace PostSharp.Toolkit.Domain.Controls
{
    public class RedoButton : UndoRedoButtonBase
    {
        public RedoButton()
        {
            this.DefaultStyleKey = typeof(RedoButton);
        }

        protected override void SetOperations()
        {
            this.Operations = this.HistoryTracker.RedoOperations.Reverse().Take(this.MaxOperationsCount);
            this.OnPropertyChanged("Operations");
        }

        public override ICommand RevertOperationCommand
        {
            get
            {
                return this.HistoryTracker != null ? new UndoRedoCommand(this, false) : null;
            }
        }

        protected override void OnClickCore()
        {
            if (this.HistoryTracker != null && this.HistoryTracker.CanRedo())
            {
                this.HistoryTracker.Redo();
            }
        }
    }
}