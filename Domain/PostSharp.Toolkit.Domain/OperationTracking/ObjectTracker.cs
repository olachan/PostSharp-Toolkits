using System;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public abstract class ObjectTracker : Tracker, IObjectTracker
    {
        protected ITrackable Target;

        protected BatchOperation CurrentChunk;

        protected ChunkToken CurrentChunkToken;

        protected ObjectTracker(ITrackable target)
        {
            this.Target = target;
        }

        public virtual void SetParentTracker(Tracker tracker)
        {
            this.ParentTracker = tracker;
        }

        public ObjectTrackingChunkToken GetNewChunkToken()
        {
            return new ObjectTrackingChunkToken( this );
        }

        public ChunkToken StartNamedChunk()
        {
            if (CurrentChunkToken != null)
            {
                throw new NotSupportedException("multiple named chunks are not supported");
            }

            this.StartChunk();

            CurrentChunkToken = new ChunkToken();

            return CurrentChunkToken;
        }

        public void EndNamedChunk(ChunkToken token)
        {
            if (!ReferenceEquals(CurrentChunkToken, token))
            {
                throw new ArgumentException("passed token does not match current chunk's token");
            }

            CurrentChunkToken = null;

            this.EndChunk();
        }

        public virtual void StartChunk()
        {
            if (DisableCollectingData || CurrentChunkToken != null)
            {
                return;
            }

            if (CurrentChunk != null)
            {
                this.EndChunk();
            }

            CurrentChunk = new BatchOperation();
        }

        public virtual void EndChunk()
        {
            if (DisableCollectingData || CurrentChunkToken != null)
            {
                return;
            }

            if (CurrentChunk != null && CurrentChunk.OpertaionCount > 0)
            {
                this.AddOperation(CurrentChunk);
            }

            CurrentChunk = null;
        }

        public virtual bool IsChunkActive
        {
            get
            {
                return this.CurrentChunk != null;
            }
        }

        public int OperationCount
        {
            get
            {
                return this.UndoOperations.Count;
            }
        }

        public virtual void AddOperationToChunk(IOperation operation)
        {
            if (DisableCollectingData)
            {
                return;
            }

            this.CurrentChunk.AddOperation(operation);
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
                        snapshots.Select(s => (IOperation)(new InvertOperationWrapper(s))).ToList()));
            }

        }

        internal virtual void SetOperationCollections(IOperationCollection undoOperations, IOperationCollection redoOperations)
        {
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
        }
    }

    public class ChunkToken
    {
    }
}