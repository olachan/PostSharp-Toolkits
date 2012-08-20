using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal class DelegateSnapshot<TTarget> : Snapshot
        where TTarget : class, ITrackable 
    {
        public Action<TTarget> UndoAction { get; set; }

        public Action<TTarget> RedoAction { get; set; }

        public DelegateSnapshot(TTarget target, Action<TTarget> undoAction, Action<TTarget> redoAction)
            : base( target )
        {
            this.UndoAction = undoAction;
            this.RedoAction = redoAction;
        }

        public DelegateSnapshot(TTarget target, Action<TTarget> undoAction, Action<TTarget> redoAction, string restorePointName)
            : base( target, restorePointName )
        {
            this.UndoAction = undoAction;
            this.RedoAction = redoAction;
        }

        public override ISnapshot Restore()
        {
            TTarget sot = this.Target.Target as TTarget;
            if ( sot != null )
            {
                this.UndoAction( sot );
            }

            return new DelegateSnapshot<TTarget>( sot, this.RedoAction, this.UndoAction );
        }
    }
}