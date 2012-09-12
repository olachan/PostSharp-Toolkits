using System;

using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain
{
    internal class DelegateOperation : Operation
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

        protected internal override void Undo()
        {
            this.UndoAction();
        }

        protected internal override void Redo()
        {
            this.RedoAction();
        }
    }
}