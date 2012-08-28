using System;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public sealed class ObjectTracker : Tracker, IObjectTracker
    {
        private ITrackable target;

        private ComplexOperation currentChunk;

        private ChunkToken currentChunkToken;

        public ObjectTracker(ITrackable target)
        {
            this.target = target;
        }

        public void Clear()
        {
            IOperationCollection undoOperations = this.UndoOperations.Clone();
            IOperationCollection redoOperations = this.RedoOperations.Clone();

            this.AddUndoOperationToParentTracker(new List<IOperation>(), undoOperations, redoOperations);

            this.UndoOperations.Clear();
            this.RedoOperations.Clear();
        }

        public void SetParentTracker(Tracker tracker)
        {
            this.ParentTracker = tracker;
        }

        public ObjectTrackingChunkToken GetNewChunkToken()
        {
            return new ObjectTrackingChunkToken( this );
        }

        public ChunkToken StartNamedChunk()
        {
            if (this.currentChunkToken != null)
            {
                throw new NotSupportedException("multiple named chunks are not supported");
            }

            this.StartChunk();

            this.currentChunkToken = new ChunkToken();

            return this.currentChunkToken;
        }

        public void EndNamedChunk(ChunkToken token)
        {
            if (!ReferenceEquals(this.currentChunkToken, token))
            {
                throw new ArgumentException("passed token does not match current chunk's token");
            }

            this.currentChunkToken = null;

            this.EndChunk();
        }

        public void StartChunk()
        {
            if (this.DisableCollectingData || this.currentChunkToken != null)
            {
                return;
            }

            if (this.currentChunk != null)
            {
                this.EndChunk();
            }

            this.currentChunk = new ComplexOperation();
        }

        public void EndChunk()
        {
            if (this.DisableCollectingData || this.currentChunkToken != null)
            {
                return;
            }

            if (this.currentChunk != null && this.currentChunk.OperationCount > 0)
            {
                this.AddOperation(this.currentChunk);
            }

            this.currentChunk = null;
        }

        public void AddToCurrentOperation( ISubOperation operation )
        {
            if (this.DisableCollectingData)
            {
                return;
            }

            this.currentChunk.AddOperation(operation);
        }

        public bool IsChunkActive
        {
            get
            {
                return this.currentChunk != null;
            }
        }

        public int OperationCount
        {
            get
            {
                return this.UndoOperations.Count;
            }
        }

        protected override void AddUndoOperationToParentTracker(List<IOperation> operations, IOperationCollection undoOperations, IOperationCollection redoOperations)
        {
            if (this.ParentTracker != null)
            {
                this.ParentTracker.AddOperation(
                    new ObjectTrackerOperation(
                        this,
                        undoOperations,
                        redoOperations,
                        operations.Where( o => o != null ).Select(s => (IOperation)(new InvertOperationWrapper(s))).ToList()));
            }

        }

        internal void SetOperationCollections(IOperationCollection undoOperations, IOperationCollection redoOperations)
        {
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
        }
    }

    public class ChunkToken
    {
    }
}