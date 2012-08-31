using System;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal sealed class AggregateTracker : Tracker, IObjectTracker
    {
        private ComplexOperation currentOperation;

        private AtomicOperationToken currentAtomicOperationToken;

        private int implicitOperationNestingCounter;

        public AggregateTracker(object aggregateRoot)
        {
            this.AggregateRoot = aggregateRoot;
            this.implicitOperationNestingCounter = 0;
        }

        public object AggregateRoot { get; private set; }

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

        public void Clear()
        {
            OperationCollection undoOperations = this.UndoOperations.Clone();
            OperationCollection redoOperations = this.RedoOperations.Clone();

            this.AddUndoOperationToParentTracker(new List<IOperation>(), undoOperations, redoOperations);

            this.UndoOperations.Clear();
            this.RedoOperations.Clear();
        }

        internal void SetOperationCollections(OperationCollection undoOperations, OperationCollection redoOperations)
        {
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
        }

        public void AssociateWithParent(Tracker globalTracker)
        {
            this.ParentTracker = globalTracker;
        }

        public void AddToCurrentOperation(ISubOperation operation)
        {
            if (!this.IsTrackingEnabled)
            {
                return;
            }

            if (this.currentOperation == null)
            {
                throw new InvalidOperationException("Can not add to current operation. There is no operation opened");
            }

            this.currentOperation.AddOperation(operation);
        }

        public override RestorePointToken AddRestorePoint(string name = null)
        {
            if (this.currentAtomicOperationToken != null)
            {
                throw new NotSupportedException("Adding restore point inside atomic operation is not supported");
            }

            // if there is open implicit operation end it and after adding restore point start new one.
            this.EndOperation();

            var restorePoint = this.UndoOperations.AddRestorePoint(name);
            
            this.StartOperation();

            return restorePoint;
        }

        public IDisposable StartAtomicOperation()
        {
            return new AtomicOperationToken(this);
        }

        private void StartExplicitOperation(AtomicOperationToken token)
        {
            if (this.currentAtomicOperationToken != null)
            {
                //TODO: Some support for multiple operations?
                throw new NotSupportedException("Operation already started");
            }

            if (!this.IsTrackingEnabled)
            {
                throw new NotSupportedException("Cannot start explicit operation while tracking is disabled");
            }

            StartOperation();

            this.currentAtomicOperationToken = token;
        }

        private void EndExplicitOperation(AtomicOperationToken token)
        {
            if (this.currentAtomicOperationToken != token)
            {
                //TODO: Some support for multiple operations?
                throw new NotSupportedException("Invalid state! Nested operations are not currently supported");
            }

            if (!this.IsTrackingEnabled)
            {
                throw new NotSupportedException("Cannot end explicit operation while tracking is disabled");
            }

            EndOperation();

            this.currentAtomicOperationToken = null;

            // restore implicit operation if it was opened before starting atomic operation
            if (this.implicitOperationNestingCounter > 0)
            {
                this.StartOperation();
            }
        }

        public IDisposable StartImplicitOperation()
        {
            return new ImplicitOperationToken(this);
        }

        private void StartImplicitOperationInternal(ImplicitOperationToken token)
        {
            if (!this.IsTrackingEnabled || this.currentAtomicOperationToken != null)
            {
                return;
            }

            token.Level = this.implicitOperationNestingCounter;

            if (this.implicitOperationNestingCounter == 0)
            {
                this.StartOperation();
            }

            this.implicitOperationNestingCounter++;
        }

        private void EndImplicitOperation(ImplicitOperationToken token)
        {
            if (!this.IsTrackingEnabled || this.currentAtomicOperationToken != null)
            {
                return;
            }

            this.implicitOperationNestingCounter--;

            if (token.Level != this.implicitOperationNestingCounter)
            {
                throw new ArgumentException("Implicit operations closed in wrong order");
            }

            if (this.implicitOperationNestingCounter == 0)
            {
                this.EndOperation();
            }
        }


        private void StartOperation()
        {
            if (this.currentOperation != null)
            {
                this.EndOperation();
            }

            this.currentOperation = new ComplexOperation();
        }

        private void EndOperation()
        {
            if (this.currentOperation != null && this.currentOperation.OperationCount > 0)
            {
                this.AddOperation(this.currentOperation);
            }

            this.currentOperation = null;
        }

        internal override void AddUndoOperationToParentTracker(List<IOperation> operations, OperationCollection undoOperations, OperationCollection redoOperations)
        {
            if (this.ParentTracker != null)
            {
                ((Tracker)this.ParentTracker).AddOperation(
                    new ObjectTrackerOperation(
                        this,
                        undoOperations,
                        redoOperations,
                        operations.Where(o => o != null).Select(InvertOperationWrapper.InvertOperation).ToList()));
            }
        }

        private class AtomicOperationToken : IDisposable
        {
            private readonly AggregateTracker aggregateTracker;

            internal AtomicOperationToken(AggregateTracker aggregateTracker)
            {
                this.aggregateTracker = aggregateTracker;
                this.aggregateTracker.StartExplicitOperation(this);
            }

            public void Dispose()
            {
                this.aggregateTracker.EndExplicitOperation(this);
            }
        }

        private class ImplicitOperationToken : IDisposable
        {
            public int Level { get; set; }

            private readonly AggregateTracker aggregateTracker;

            internal ImplicitOperationToken(AggregateTracker aggregateTracker)
            {
                this.aggregateTracker = aggregateTracker;
                this.aggregateTracker.StartImplicitOperationInternal(this);
            }

            public void Dispose()
            {
                this.aggregateTracker.EndImplicitOperation(this);
            }
        }
    }
}