using System;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal sealed class ObjectTracker : Tracker, IObjectTracker
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

        private class ImplicitOperationToken : IDisposable
        {
            public int Level { get; set; }

            private readonly ObjectTracker objectTracker;

            internal ImplicitOperationToken(ObjectTracker objectTracker)
            {
                this.objectTracker = objectTracker;
                this.objectTracker.StartImplicitOperationInternal(this);
            }

            public void Dispose()
            {
                this.objectTracker.EndImplicitOperation(this);
            }
        }

        private ComplexOperation currentOperation;

        private AtomicOperationToken currentAtomicOperationToken;

        private int implicitOperationNestingCounter;

        public ObjectTracker(ITrackable target)
        {
            this.implicitOperationNestingCounter = 0;
        }

        public void Clear()
        {
            OperationCollection undoOperations = this.UndoOperations.Clone();
            OperationCollection redoOperations = this.RedoOperations.Clone();

            this.AddUndoOperationToParentTracker(new List<IOperation>(), undoOperations, redoOperations);

            this.UndoOperations.Clear();
            this.RedoOperations.Clear();
        }

        public void AssociateWithParent(ITracker globalTracker)
        {
            this.ParentTracker = globalTracker;
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

            if (this.IsTrackingDisabled)
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

            if (this.IsTrackingDisabled)
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
            if (this.IsTrackingDisabled || this.currentAtomicOperationToken != null)
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
            if (this.IsTrackingDisabled || this.currentAtomicOperationToken != null)
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

        public void AddToCurrentOperation(ISubOperation operation)
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

        internal void SetOperationCollections(OperationCollection undoOperations, OperationCollection redoOperations)
        {
            this.UndoOperations = undoOperations;
            this.RedoOperations = redoOperations;
        }
    }
}