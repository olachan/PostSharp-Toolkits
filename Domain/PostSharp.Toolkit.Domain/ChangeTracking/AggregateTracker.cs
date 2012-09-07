using System;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal sealed class AggregateTracker : Tracker, IObjectTracker
    {
        private ComplexOperation currentOperation;

        private AtomicOperationScope currentAtomicOperationScope;

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
                return this.UndoOperationCollection.Count;
            }
        }

        internal void SetOperationCollections(OperationCollection undoOperations, OperationCollection redoOperations)
        {
            this.UndoOperationCollection = undoOperations;
            this.RedoOperationCollection = redoOperations;
        }

        public void AssociateWithParent(Tracker globalTracker)
        {
            this.ParentTracker = globalTracker;
        }

        public void AddToCurrentOperation(SubOperation operation)
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

        protected override bool AddOperationEnabledCheck( bool throwException = true )
        {
            if (!this.IsTrackingEnabled)
            {
                if (throwException)
                {
                    throw new InvalidOperationException("Can not add operation to disabled tracker");
                }

                return false;
            }

            return true;
        }

        protected override bool AddRestorePointEnabledCheck( bool throwException = true )
        {
            if (!this.IsTrackingEnabled)
            {
                if (throwException)
                {
                    throw new InvalidOperationException("Can not add restore point to disabled tracker");
                }

                return false;
            }

            return true;
        }

        protected override bool UndoRedoOperationEnabledCheck( bool throwException = true )
        {
            if (!this.IsTrackingEnabled)
            {
                if (throwException)
                {
                    throw new InvalidOperationException("Can not perform operations on disabled tracker");
                }

                return false;
            }

            return true;
        }

        public override RestorePointToken AddRestorePoint(string name = null)
        {
            if (this.currentAtomicOperationScope != null)
            {
                throw new NotSupportedException("Adding restore point inside atomic operation is not supported");
            }

            // if there is open implicit operation end it and after adding restore point start new one.
            this.EndOperation();

            var restorePoint = this.UndoOperationCollection.AddRestorePoint(name);
            
            this.StartOperation( name );

            return restorePoint;
        }

        public IDisposable StartAtomicOperation(string name)
        {
            return new AtomicOperationScope(this, name);
        }

        private void StartExplicitOperation(AtomicOperationScope scope)
        {
            if (this.currentAtomicOperationScope != null)
            {
                //TODO: Some support for multiple operations?
                throw new NotSupportedException("Operation already started");
            }

            if (!this.IsTrackingEnabled)
            {
                throw new NotSupportedException("Cannot start explicit operation while tracking is disabled");
            }

            this.StartOperation( scope.Name );

            this.currentAtomicOperationScope = scope;
        }

        private void EndExplicitOperation(AtomicOperationScope scope)
        {
            if (this.currentAtomicOperationScope != scope)
            {
                //TODO: Some support for multiple operations?
                throw new NotSupportedException("Invalid state! Nested operations are not currently supported");
            }

            if (!this.IsTrackingEnabled)
            {
                throw new NotSupportedException("Cannot end explicit operation while tracking is disabled");
            }

            EndOperation();

            this.currentAtomicOperationScope = null;

            // restore implicit operation if it was opened before starting atomic operation
            if (this.implicitOperationNestingCounter > 0)
            {
                this.StartOperation( scope.Name );
            }
        }

        public IDisposable StartImplicitOperationScope(string name)
        {
            return new ImplicitOperationScope(this, name);
        }

        private void StartImplicitOperation( ImplicitOperationScope scope )
        {
            if (!this.IsTrackingEnabled || this.currentAtomicOperationScope != null)
            {
                return;
            }

            scope.Level = this.implicitOperationNestingCounter;

            if (this.implicitOperationNestingCounter == 0)
            {
                this.StartOperation(scope.Name);
            }

            this.implicitOperationNestingCounter++;
        }

        private void EndImplicitOperation(ImplicitOperationScope scope)
        {
            if (!this.IsTrackingEnabled || this.currentAtomicOperationScope != null)
            {
                return;
            }

            this.implicitOperationNestingCounter--;

            if (scope.Level != this.implicitOperationNestingCounter)
            {
                throw new ArgumentException("Implicit operations closed in wrong order");
            }

            if (this.implicitOperationNestingCounter == 0)
            {
                this.EndOperation();
            }
        }


        private void StartOperation( string name )
        {
            if (this.currentOperation != null)
            {
                this.EndOperation();
            }

            this.currentOperation = new ComplexOperation(name);
        }

        private void EndOperation()
        {
            if (this.currentOperation != null && this.currentOperation.OperationCount > 0)
            {
                this.AddOperation(this.currentOperation);
            }

            this.currentOperation = null;
        }

        internal override void AddUndoOperationToParentTracker( string name, List<Operation> operations, OperationCollection undoOperations, OperationCollection redoOperations )
        {
            if (this.ParentTracker != null)
            {
                this.ParentTracker.AddOperation(
                    new ObjectTrackerOperation(
                        this,
                        name,
                        undoOperations,
                        redoOperations,
                        operations.Where(o => o != null).Select(o => InvertOperationWrapper.InvertOperation(o, this.NameGenerationConfiguration.UndoOperationStringFormat)).ToList()));
            }
        }

        public override void StopTracking()
        {
            base.StopTracking();

            this.currentOperation = null;
            this.currentAtomicOperationScope = null;
        }

        private sealed class AtomicOperationScope : IDisposable
        {
            private readonly AggregateTracker aggregateTracker;

            internal AtomicOperationScope( AggregateTracker aggregateTracker, string name )
            {
                this.aggregateTracker = aggregateTracker;
                this.Name = name;
                this.aggregateTracker.StartExplicitOperation(this);
            }

            public string Name { get; private set; }

            public void Dispose()
            {
                this.aggregateTracker.EndExplicitOperation(this);
            }
        }

        private sealed class ImplicitOperationScope : IDisposable
        {
            public string Name { get; private set; }

            public int Level { get; set; }

            private readonly AggregateTracker aggregateTracker;

            internal ImplicitOperationScope( AggregateTracker aggregateTracker, string name )
            {
                Name = name;
                this.aggregateTracker = aggregateTracker;
                this.aggregateTracker.StartImplicitOperation(this);
            }

            public void Dispose()
            {
                this.aggregateTracker.EndImplicitOperation(this);
            }
        }
    }
}