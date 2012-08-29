using System;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public sealed class ObjectTracker : Tracker, IObjectTracker
    {
        private class AtomicOperationToken : IDisposable
        {
            private readonly ObjectTracker objectTracker;

            internal AtomicOperationToken(ObjectTracker objectTracker)
            {
                this.objectTracker = objectTracker;
                this.objectTracker.StartExplicitOperation(this);
            }

            public void Dispose()
            {
                this.objectTracker.EndExplicitOperation(this);
            }
        }

        private ITrackable target;

        private ComplexOperation currentOperation;

        private AtomicOperationToken currentOperationToken;

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

        public void AssociateWithParent( ITracker globalTracker )
        {
            this.ParentTracker = globalTracker;
        }

        public void SetParentTracker(Tracker tracker)
        {
            this.ParentTracker = tracker;
        }

        public IDisposable StartAtomicOperation()
        {
            return new AtomicOperationToken( this );
        }

        private void StartExplicitOperation(AtomicOperationToken token)
        {
            if (this.currentOperationToken != null)
            {
                //TODO: Some support for multiple operations?
                throw new NotSupportedException("Operation already started");
            }

            if (this.IsTrackingDisabled)
            {
                throw new NotSupportedException("Cannot start explicit operation while tracking is disabled");
            }

            StartOperation();

            this.currentOperationToken = token;
        }

        private void StartOperation()
        {
            if ( this.currentOperation != null )
            {
                this.EndOperation();
            }

            this.currentOperation = new ComplexOperation();
        }

        public void StartImplicitOperation()
        {
            if (this.IsTrackingDisabled || this.currentOperationToken != null)
            {
                return;
            }

            this.StartOperation();
        }

        private void EndExplicitOperation(AtomicOperationToken token)
        {
            if (this.currentOperationToken != token)
            {
                //TODO: Some support for multiple operations?
                throw new NotSupportedException("Invalid state! Nested operations are not currently supported");
            }

            if (this.IsTrackingDisabled)
            {
                throw new NotSupportedException("Cannot end explicit operation while tracking is disabled");
            }

            EndOperation();

            this.currentOperationToken = null;
        }

        public void EndImplicitOperation()
        {
            if (this.IsTrackingDisabled || this.currentOperationToken != null)
            {
                return;
            }

            this.EndOperation();
        }

        private void EndOperation()
        {
            if ( this.currentOperation != null && this.currentOperation.OperationCount > 0 )
            {
                this.AddOperation( this.currentOperation );
            }

            this.currentOperation = null;
        }

        public void AddToCurrentOperation( ISubOperation operation )
        {
            //TODO: what if there is no operation open?

            if (this.IsTrackingDisabled)
            {
                return;
            }

            this.currentOperation.AddOperation(operation);
        }

        public bool IsOperationOpen
        {
            get
            {
                return this.currentOperation != null;
            }
        }

        public int OperationsCount
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
}