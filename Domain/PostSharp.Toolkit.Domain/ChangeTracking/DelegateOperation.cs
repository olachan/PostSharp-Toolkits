using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public class DelegateOperation: IOperation
    {
        public Action UndoAction { get; set; }

        public Action RedoAction { get; set; }

        public DelegateOperation(Action undoAction, Action redoAction)
        {
            this.UndoAction = undoAction;
            this.RedoAction = redoAction;
        }

        public DelegateOperation(Action undoAction, Action redoAction, string name)
        {
            this.Name = name;
            this.UndoAction = undoAction;
            this.RedoAction = redoAction;
        }

        public void Undo()
        {
                this.UndoAction();
        }

        public void Redo()
        {
                this.RedoAction();
        }

        public string Name { get; private set; }
    }
}