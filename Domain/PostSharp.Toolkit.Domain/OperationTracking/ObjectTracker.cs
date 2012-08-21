using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public abstract class ObjectTracker : Tracker, IObjectTracker
    {
        protected ITrackable Target;

        protected BatchOperation CurrentChunk;

        private int currentChunkCounter;

        protected ObjectTracker(ITrackable target)
        {
            this.Target = target;
            this.currentChunkCounter = 0;
        }

        public virtual void SetParentTracker(Tracker tracker)
        {
            this.ParentTracker = tracker;
        }

        // TODO check if CurrentChunk is ended
        public virtual void StartChunk()
        {
            if (DisableCollectingData)
            {
                return;
            }

            this.currentChunkCounter++;

            if (CurrentChunk == null)
            {
                CurrentChunk = new BatchOperation();
            }
        }

        public virtual void EndChunk()
        {
            if (DisableCollectingData)
            {
                return;
            }

            this.currentChunkCounter--;

            if (currentChunkCounter == 0)
            {
                if ( CurrentChunk != null && CurrentChunk.OpertaionCount > 0 )
                {
                    this.AddOperation( CurrentChunk );
                }

                CurrentChunk = null;
            }
        }

        public virtual bool IsChunkActive
        {
            get
            {
                return this.CurrentChunk != null;
            }
        }

        public virtual void AddOperationToChunk(IOperation operation)
        {
            if (DisableCollectingData)
            {
                return;
            }

            this.CurrentChunk.AddOperation( operation );
        }

        protected override void AddUndoOperationToParentTracker(List<IOperation> snapshots, IOperationCollection undoOperations, IOperationCollection redoOperations)
        {
            if (this.ParentTracker != null)
            {
                this.ParentTracker.AddOperation(
                    new ObjectTrackerOperation(
                        this,
                        undoOperations,
                        redoOperations,
                        snapshots.Select( s => (IOperation)(new InvertOperationWrapper( s )) ).ToList() ) );
            }

        }

        internal virtual void SetOperationCollections(IOperationCollection undoOperations, IOperationCollection redoOperations)
        {
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
        }
    }
}