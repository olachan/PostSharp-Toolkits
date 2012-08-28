using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: Don't like the name
    //TODO: Do we really need it? We need public DelegateOperation with no target anyway...
    internal class TargetedDelegateOperation<TTarget> : TargetedOperation
        where TTarget : class, ITrackable 
    {
        public Action<TTarget> UndoAction { get; set; }

        public Action<TTarget> RedoAction { get; set; }

        public TargetedDelegateOperation(TTarget target, Action<TTarget> undoAction, Action<TTarget> redoAction)
            : base( target )
        {
            this.UndoAction = undoAction;
            this.RedoAction = redoAction;
        }

        public TargetedDelegateOperation(TTarget target, Action<TTarget> undoAction, Action<TTarget> redoAction, string name)
            : base( target, name )
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