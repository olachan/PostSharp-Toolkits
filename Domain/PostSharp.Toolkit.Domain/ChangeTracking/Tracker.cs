#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using PostSharp.Toolkit.Threading;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [ThreadUnsafeObject]
    public abstract class Tracker : ITracker, IDisposable
    {
        internal OperationCollection UndoOperationCollection;

        internal OperationCollection RedoOperationCollection;

        protected Tracker(bool enableTracking = false)
        {
            this.UndoOperationCollection = new OperationCollection();
            this.RedoOperationCollection = new OperationCollection();
            this.IsTrackingInternal = true;
            this.IsTracking = enableTracking;
        }

        public Tracker ParentTracker { get; protected set; }


        // internal field used to temporary disable tracking (eg during performing undo/redo operations)

        protected bool IsTrackingInternal;

        // public property used to permanently disable tracking
        public bool IsTracking { get; private set; }

        protected bool IsTrackingEnabled
        {
            get
            {
                return this.IsTracking && this.IsTrackingInternal;
            }
        }

        private OperationNameGenerationConfiguration operationNameGenerationConfiguration;

        public OperationNameGenerationConfiguration NameGenerationConfiguration
        {
            get
            {
                if (this.operationNameGenerationConfiguration != null)
                {
                    return this.operationNameGenerationConfiguration;
                }

                if (this.ParentTracker != null)
                {
                    return this.ParentTracker.NameGenerationConfiguration;
                }

                return OperationNameGenerationConfiguration.Default;
            }
            set
            {
                this.operationNameGenerationConfiguration = value;
            }
        }

        public bool RestorePointExists(string restorePoint)
        {
            return this.UndoOperationCollection.RestorePointExists(restorePoint);
        }

        public bool RestorePointExists(RestorePointToken restorePoint)
        {
            return this.UndoOperationCollection.RestorePointExists(restorePoint);
        }

        public void TrimHistory(int count)
        {
            this.UndoOperationCollection.Trim(count);
        }

        public void TrimHistory(string restorePoint)
        {
            this.UndoOperationCollection.Trim(restorePoint);
        }

        public void TrimRedo(int count)
        {
            this.RedoOperationCollection.Trim(count);
        }

        public void TrimRedo(string restorePoint)
        {
            this.RedoOperationCollection.Trim(restorePoint);
        }

        protected abstract bool AddOperationEnabledCheck(bool throwException = true);

        protected abstract bool AddRestorePointEnabledCheck(bool throwException = true);

        protected abstract bool UndoRedoOperationEnabledCheck(bool throwException = true);

        public virtual void AddOperation(Operation operation, bool addToParent = true)
        {
            if (!this.AddOperationEnabledCheck())
            {
                return;
            }

            if (addToParent && this.ParentTracker != null)
            {
                this.ParentTracker.AddOperation(new TrackerDelegateOperation(this, () => this.Undo(false), () => this.Redo(false), operation.Name));
            }

            this.UndoOperationCollection.Push(operation);
            this.RedoOperationCollection.Clear();
        }

        public virtual RestorePointToken AddRestorePoint(string name = null)
        {
            if (!this.AddRestorePointEnabledCheck())
            {
                return null;
            }

            return this.UndoOperationCollection.AddRestorePoint(name);
        }

        public bool CanUndo()
        {
            return this.UndoRedoOperationEnabledCheck(false);
        }

        public void Undo()
        {
            this.Undo(true);
        }

        protected virtual void Undo(bool addToParent)
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
                undoOperations = this.UndoOperationCollection.Clone();
                redoOperations = this.RedoOperationCollection.Clone();
            }

            var operation = this.UndoOperationCollection.Pop();
            if (operation == null)
            {
                return;
            }

            if (addToParent)
            {
                this.AddUndoOperationToParentTracker( string.Format(this.NameGenerationConfiguration.UndoOperationStringFormat, operation.Name), operation, undoOperations, redoOperations );
            }

            operation.Undo();
            this.RedoOperationCollection.Push(operation);

            if (operation.IsRestorePoint())
            {
                this.UndoInternal(addToParent);
            }
        }

        public virtual void UndoToOperation(Operation operation)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.UndoOperationCollection.GetOperationsToRestorePoint(operation),
                this.RedoOperationCollection.Push,
                string.Format(this.NameGenerationConfiguration.UndoToStringFormat, operation.Name));
        }

        public virtual void UndoTo(string name)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.UndoOperationCollection.GetOperationsToRestorePoint(name),
                this.RedoOperationCollection.Push,
                string.Format(this.NameGenerationConfiguration.UndoToStringFormat, name));
        }

        public virtual void UndoTo(RestorePointToken token)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.UndoOperationCollection.GetOperationsToRestorePoint(token),
                this.RedoOperationCollection.Push,
                string.Format(this.NameGenerationConfiguration.UndoToStringFormat, token.Name));
        }

        private void UndoToRestorePoint(Func<Stack<Operation>> getOperationsToRestorePoint, Action<Operation> pushToRedoAction, string name)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            using (this.StartDisabledTrackingScope())
            {
                OperationCollection undoOperations = this.UndoOperationCollection.Clone();
                OperationCollection redoOperations = this.RedoOperationCollection.Clone();

                Stack<Operation> operationsToUndo = getOperationsToRestorePoint();

                var operationsForParentTracker = operationsToUndo.ToList();
                operationsForParentTracker.Reverse();

                this.AddUndoOperationToParentTracker( name, operationsForParentTracker, undoOperations, redoOperations );

                while (operationsToUndo.Count > 0)
                {
                    Operation operation = operationsToUndo.Pop();
                    operation.Undo();
                    pushToRedoAction(operation);
                }
            }
        }

        public bool CanRedo()
        {
            return this.UndoRedoOperationEnabledCheck(false);
        }

        public void Redo()
        {
            this.Redo(true);
        }

        protected virtual void Redo(bool addToParent)
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
                undoOperations = this.UndoOperationCollection.Clone();
                redoOperations = this.RedoOperationCollection.Clone();
            }

            var operation = this.RedoOperationCollection.Pop();

            if (operation == null)
            {
                return;
            }

            if (addToParent)
            {
                this.AddUndoOperationToParentTracker( string.Format(this.NameGenerationConfiguration.RedoOperationStringFormat, operation.Name), operation, undoOperations, redoOperations );
            }

            operation.Redo();
            this.UndoOperationCollection.Push(operation);

            if (operation.IsRestorePoint())
            {
                this.RedoInternal(addToParent);
            }
        }

        public virtual void RedoTo(string name)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.RedoOperationCollection.GetOperationsToRestorePoint(name),
                this.UndoOperationCollection.Push,
                string.Format(this.NameGenerationConfiguration.RedoToStringFormat, name));
        }

        public virtual void RedoTo(RestorePointToken token)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.RedoOperationCollection.GetOperationsToRestorePoint(token),
                this.UndoOperationCollection.Push,
                string.Format(this.NameGenerationConfiguration.RedoToStringFormat, token.Name));
        }

        public virtual void RedoToOperation(Operation operation)
        {
            if (!this.UndoRedoOperationEnabledCheck())
            {
                return;
            }

            this.UndoToRestorePoint(
                () => this.RedoOperationCollection.GetOperationsToRestorePoint(operation),
                this.UndoOperationCollection.Push,
                string.Format(this.NameGenerationConfiguration.RedoToStringFormat, operation.Name));
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

        internal abstract void AddUndoOperationToParentTracker( string name, List<Operation> operations, OperationCollection undoOperations, OperationCollection redoOperations );

        internal virtual void AddUndoOperationToParentTracker( string name, Operation operation, OperationCollection undoOperations, OperationCollection redoOperations )
        {
            this.AddUndoOperationToParentTracker( name, new List<Operation> { operation }, undoOperations, redoOperations );
        }

        internal bool ContainsReferenceTo(Tracker tracker)
        {
            return this.UndoOperationCollection.ContainsReferenceTo(tracker) || this.RedoOperationCollection.ContainsReferenceTo(tracker);
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

            this.UndoOperationCollection.Clear();
            this.RedoOperationCollection.Clear();
        }

        public virtual bool CanStopTracking()
        {
            return this.ParentTracker == null || !this.ParentTracker.ContainsReferenceTo(this);
        }

        private int maximumOperationsCount;

        public int MaximumOperationsCount
        {
            get
            {
                return this.maximumOperationsCount;
            }
            set
            {
                this.UndoOperationCollection.MaximumOperationsCount = value;
                this.RedoOperationCollection.MaximumOperationsCount = value;
                this.maximumOperationsCount = value;
            }
        }

        public void Clear()
        {
            OperationCollection undoOperations = this.UndoOperationCollection.Clone();
            OperationCollection redoOperations = this.RedoOperationCollection.Clone();

            this.AddUndoOperationToParentTracker( "Clear", new List<Operation>(), undoOperations, redoOperations );

            this.UndoOperationCollection.Clear();
            this.RedoOperationCollection.Clear();
        }

        public virtual void Dispose()
        {
            this.UndoOperationCollection.Clear();
            this.RedoOperationCollection.Clear();
            this.IsTracking = false;
            this.IsTrackingInternal = false;
        }

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
                if (this.enableTrackingOnDispose)
                {
                    this.tracker.EnableTrackingInternal();
                }
            }
        }
    }
}