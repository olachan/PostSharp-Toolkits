using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public class DelegateOperation<TTarget> : Operation
        where TTarget : class, ITrackable 
    {
        public Action<TTarget> UndoAction { get; set; }

        public Action<TTarget> RedoAction { get; set; }

        public DelegateOperation(TTarget target, Action<TTarget> undoAction, Action<TTarget> redoAction)
            : base( target )
        {
            this.UndoAction = undoAction;
            this.RedoAction = redoAction;
        }

        public DelegateOperation(TTarget target, Action<TTarget> undoAction, Action<TTarget> redoAction, string restorePointName)
            : base( target, restorePointName )
        {
            this.UndoAction = undoAction;
            this.RedoAction = redoAction;
        }

        public override void Undo()
        {
            TTarget sot = this.Target as TTarget;
            if ( sot != null )
            {
                this.UndoAction( sot );
            }
        }

        public override void Redo()
        {
            TTarget sot = this.Target as TTarget;
            if (sot != null)
            {
                this.RedoAction(sot);
            }
        }
    }
}