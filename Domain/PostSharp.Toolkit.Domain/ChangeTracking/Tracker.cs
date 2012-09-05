#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using PostSharp.Toolkit.Threading;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [ThreadUnsafeObject]
    public abstract class Tracker : ITracker, IDisposable
    {
        protected sealed class TrackingDisabledScope : IDisposable
        {
            private readonly Tracker tracker;

            private readonly bool enableTrackingOnDispose;

            public TrackingDisabledScope(Tracker tracker)
            {
                this.tracker = tracker;
                this.enableTrackingOnDispose = tracker.IsTrackingEnabled;
                tracker.DisableTrackingInternal();
            }

            public void Dispose()
            {
                if (enableTrackingOnDispose)
                {
                    tracker.EnableTrackingInternal();
                }
            }
        }

        internal OperationCollection UndoOperations;

        internal OperationCollection RedoOperations;

        public Tracker ParentTracker { get; protected set; }

        protected bool IsTrackingEnabled
        {
            get
            {
                return this.IsTracking && this.IsTrackingInternal;
            }
        }

        // internal field used to temporary disable tracking (eg during performing undo/redo operations)
        protected bool IsTrackingInternal;

        private int maximumOperationsCount;

        private OperationNameGenerationConfiguration operationNameGenerationConfiguration;

        // public property used to permanently disable tracking
        public bool IsTracking { get; private set; }

        protected Tracker()
        {
            this.UndoOperations = new OperationCollection();
            this.RedoOperations = new OperationCollection();
            this.IsTrackingInternal = true;
            this.IsTracking = true;
        }

        protected abstract bool AddOperationEnabledCheck();

        protected abstract bool AddRestorePointEnabledCheck();

        protected abstract bool UndoRedoOperationEnabledCheck();

        public virtual void AddOperation(IOperation operation, bool addToParent = true)
        {
            if (!this.AddOperationEnabledCheck())
            {
                return;
            }

            if (addToParent && this.ParentTracker != null)
            {
                this.ParentTracker.AddOperation(new TrackerDelegateOperation(this, () => this.Undo(false), () => this.Redo(false)));
            }

            this.UndoOperations.Push(operation);
            this.RedoOperations.Clear();
        }

        public virtual RestorePointToken AddRestorePoint(string name = null)
        {
            if (!this.AddRestorePointEnabledCheck())
            {
                return null;
            }

            return this.UndoOperations.AddRestorePoint(name);
        }

        public void Undo()
        {
            this.Undo(true);
        }

        public virtual void Undo(bool addToParent)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            using (this.StartDisabledTrackingScope())
            {
                this.UndoInternal(addToParent);
            }
        }

        private void UndoInternal(bool addToParent)
        {
            OperationCollection undoOperations = null;
            OperationCollection redoOperations = null;

            if (addToParent)
            {
                undoOperations = this.UndoOperations.Clone();
                redoOperations = this.RedoOperations.Clone();
            }

            var operation = this.UndoOperations.Pop();
            if (operation == null)
            {
                return;
            }

            if (addToParent)
            {
                this.AddUndoOperationToParentTracker(
                    operation, 
                    undoOperations, 
                    redoOperations, 
                    string.Format(this.OperationNameGenerationConfiguration.UndoOperationStringFormat, operation.Name));
            }

            operation.Undo();
            this.RedoOperations.Push(operation);

            if (operation.IsRestorePoint())
            {
                this.UndoInternal(addToParent);
            }
        }

        public void Redo()
        {
            this.Redo(true);
        }

        public virtual void Redo(bool addToParent)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            using (this.StartDisabledTrackingScope())
            {
                this.RedoInternal(addToParent);
            }
        }

        private void RedoInternal(bool addToParent)
        {
            OperationCollection undoOperations = null;
            OperationCollection redoOperations = null;

            if (addToParent)
            {
                undoOperations = this.UndoOperations.Clone();
                redoOperations = this.RedoOperations.Clone();
            }

            var operation = this.RedoOperations.Pop();

            if (operation == null)
            {
                return;
            }

            if (addToParent)
            {
                this.AddUndoOperationToParentTracker(
                    operation,
                    undoOperations, 
                    redoOperations, 
                    string.Format(this.OperationNameGenerationConfiguration.RedoOperationStringFormat, operation.Name));
            }

            operation.Redo();
            this.UndoOperations.Push(operation);

            if (operation.IsRestorePoint())
            {
                this.RedoInternal(addToParent);
            }
        }

        public virtual void UndoTo(string name)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.UndoOperations.GetOperationsToRestorePoint(name), 
                this.RedoOperations.Push,
                string.Format( this.OperationNameGenerationConfiguration.UndoToStringFormat, name ));
        }

        public virtual void UndoTo(RestorePointToken token)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.UndoOperations.GetOperationsToRestorePoint(token),
                this.RedoOperations.Push,
                string.Format(this.OperationNameGenerationConfiguration.UndoToStringFormat, token.Name));
        }

        private void UndoToRestorePoint(Func<Stack<IOperation>> getOperationsToRestorePoint, Action<IOperation> pushToRedoAction, string name)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            using (this.StartDisabledTrackingScope())
            {
                OperationCollection undoOperations = this.UndoOperations.Clone();
                OperationCollection redoOperations = this.RedoOperations.Clone();

                Stack<IOperation> operationsToUndo = getOperationsToRestorePoint();

                var operationsForParentTracker = operationsToUndo.ToList();
                operationsForParentTracker.Reverse();

                this.AddUndoOperationToParentTracker(operationsForParentTracker, undoOperations, redoOperations, name); //TODO

                while (operationsToUndo.Count > 0)
                {
                    // TODO consider optimization
                    IOperation operation = operationsToUndo.Pop();
                    operation.Undo();
                    pushToRedoAction(operation);
                }
            }
        }

        public void RedoTo(string name)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.RedoOperations.GetOperationsToRestorePoint(name), 
                this.UndoOperations.Push,
                string.Format(this.OperationNameGenerationConfiguration.RedoToStringFormat, name));
        }

        public void RedoTo(RestorePointToken token)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.RedoOperations.GetOperationsToRestorePoint(token), 
                this.UndoOperations.Push,
                string.Format(this.OperationNameGenerationConfiguration.RedoToStringFormat, token.Name));
        }

        public IDisposable StartDisabledTrackingScope()
        {
            return new TrackingDisabledScope(this);
        }

        protected void DisableTrackingInternal()
        {
            this.IsTrackingInternal = false;
        }

        protected void EnableTrackingInternal()
        {
            this.IsTrackingInternal = true;
        }

        internal abstract void AddUndoOperationToParentTracker(
            List<IOperation> operations, OperationCollection undoOperations, OperationCollection redoOperations, string name);

        internal virtual void AddUndoOperationToParentTracker(IOperation operation, OperationCollection undoOperations, OperationCollection redoOperations, string name)
        {
            this.AddUndoOperationToParentTracker(new List<IOperation> { operation }, undoOperations, redoOperations, name);
        }

        internal bool ContainsReferenceTo(Tracker tracker)
        {
            return this.UndoOperations.ContainsReferenceTo(tracker) || this.RedoOperations.ContainsReferenceTo(tracker);
        }

        public virtual void Track()
        {
            this.IsTracking = true;
        }

        public virtual void StopTracking()
        {
            if (this.ParentTracker != null)
            {
                if (this.ParentTracker.ContainsReferenceTo(this))
                {
                    throw new InvalidOperationException("Can not stop tracking because parent tracker contains reference to operations in this tracker");
                }
            }

            this.IsTracking = false;

            this.UndoOperations.Clear();
            this.RedoOperations.Clear();

            //TODO: What about child trackers in case of HistoryTracker?
        }

        public virtual bool CanStopTracking()
        {
            return this.ParentTracker == null || !this.ParentTracker.ContainsReferenceTo(this);
        }

        public int MaximumOperationsCount
        {
            get
            {
                return this.maximumOperationsCount;
            }
            set
            {
                this.UndoOperations.MaximumOperationsCount = value;
                this.RedoOperations.MaximumOperationsCount = value;
                this.maximumOperationsCount = value;
            }
        }

        public OperationNameGenerationConfiguration OperationNameGenerationConfiguration
        {
            get
            {
                if (this.operationNameGenerationConfiguration != null)
                {
                    return this.operationNameGenerationConfiguration;
                }
                
                if (this.ParentTracker != null)
                {
                    return this.ParentTracker.OperationNameGenerationConfiguration;
                } 
                
                return OperationNameGenerationConfiguration.Default;
            }
            set
            {
                this.operationNameGenerationConfiguration = value;
            }
        }

        public void Dispose()
        {
            this.UndoOperations.Clear();
            this.RedoOperations.Clear();
            this.IsTracking = false;
            this.IsTrackingInternal = false;
        }
    }
}