#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [ChunkManagement]
    public class TrackedCollection<T> : INotifyCollectionChanged, IList<T>, IList, ITrackedObject
    {
        private readonly ObservableCollection<T> innerCollection;

        private void Initialize()
        {
            this.Tracker = new ObjectTracker(this);
        }

        public TrackedCollection()
        {
            this.Initialize();
            this.innerCollection = new ObservableCollection<T>();
        }

        public TrackedCollection(List<T> list)
        {
            this.Initialize();
            this.innerCollection = new ObservableCollection<T>(list);
        }

        public TrackedCollection(IEnumerable<T> collection)
        {
            this.Initialize();
            this.innerCollection = new ObservableCollection<T>(collection);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                this.innerCollection.CollectionChanged += value;
            }
            remove
            {
                this.innerCollection.CollectionChanged -= value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.innerCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(T item)
        {
            this.Tracker.AddToCurrentOperation( new TargetedDelegateOperation<TrackedCollection<T>>(this, c => c.Remove( item ), c => c.Add( item )) );
            this.innerCollection.Add(item);
        }

        int IList.Add(object value)
        {
            this.Tracker.AddToCurrentOperation(new TargetedDelegateOperation<TrackedCollection<T>>(this, c => ((IList)c).Remove(value), c => ((IList)c).Add(value)));
            return ((IList)this.innerCollection).Add(value);
        }

        bool IList.Contains(object value)
        {
            return ((IList)this.innerCollection).Contains(value);
        }

        public void Clear()
        {
            T[] copy = new T[this.innerCollection.Count];
            ((ICollection<T>)this.innerCollection).CopyTo(copy, 0);

            this.Tracker.AddToCurrentOperation(
               new TargetedDelegateOperation<TrackedCollection<T>>(
               this,
               d =>
               {
                   foreach (T item in copy)
                   {
                       d.Add(item);
                   }
               },
               d => d.Clear()));

            this.innerCollection.Clear();
        }

        int IList.IndexOf(object value)
        {
            return ((IList)this.innerCollection).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            this.Tracker.AddToCurrentOperation(new TargetedDelegateOperation<TrackedCollection<T>>(this, c => ((IList)c).RemoveAt(index), c => ((IList)c).Insert(index, value)));
            ((IList)this.innerCollection).Insert(index, value);
        }

        void IList.Remove(object value)
        {
            this.Tracker.AddToCurrentOperation(new TargetedDelegateOperation<TrackedCollection<T>>(this, c => ((IList)c).Add(value), c => ((IList)c).Remove(value)));
            ((IList)this.innerCollection).Remove(value);
        }

        public void RemoveAt(int index)
        {
            object value = ((IList)this)[index];
            this.Tracker.AddToCurrentOperation(new TargetedDelegateOperation<TrackedCollection<T>>(this, c => ((IList)c).Insert(index, value), c => ((IList)c).RemoveAt(index)));
            ((IList)this.innerCollection).RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return this.innerCollection[index];
            }
            set
            {
                object newValue = value;
                object oldValue = ((IList)this.innerCollection)[index];
                this.Tracker.AddToCurrentOperation(new TargetedDelegateOperation<TrackedCollection<T>>(this, c => ((IList)c)[index] = oldValue, c => ((IList)c)[index] = newValue));
                ((IList)this.innerCollection)[index] = value;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return ((IList)this.innerCollection).IsReadOnly;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return ((IList)this.innerCollection).IsReadOnly;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return ((IList)this.innerCollection).IsFixedSize;
            }
        }

        public bool Contains(T item)
        {
            return this.innerCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.innerCollection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            this.Tracker.AddToCurrentOperation(new TargetedDelegateOperation<TrackedCollection<T>>(this, c => c.Add(item), c => c.Remove(item)));
            return this.innerCollection.Remove(item);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.innerCollection).CopyTo(array, index);
        }

        public int Count
        {
            get
            {
                return this.innerCollection.Count;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((ICollection)this.innerCollection).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((ICollection)this.innerCollection).IsSynchronized;
            }
        }

        public int IndexOf(T item)
        {
            return this.innerCollection.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.Tracker.AddToCurrentOperation(new TargetedDelegateOperation<TrackedCollection<T>>(this, c => c.RemoveAt(index), c => c.Insert(index, item)));
            this.innerCollection.Insert(index, item);
        }

        public T this[int index]
        {
            get
            {
                return this.innerCollection[index];
            }
            set
            {
                T newValue = value;
                T oldValue = this.innerCollection[index];
                this.Tracker.AddToCurrentOperation(new TargetedDelegateOperation<TrackedCollection<T>>(this, c => c[index] = oldValue, c => c[index] = newValue));
                this.innerCollection[index] = value;
            }
        }

        public IObjectTracker Tracker { get; private set; }

        public void SetTracker(IObjectTracker tracker)
        {
            this.Tracker = tracker;
        }

        public int OperationCount
        {
            get
            {
                return this.Tracker.OperationsCount;
            }
        }

        //TODO: Get rid of the methods below, it may not be obvious that they do not operate on collection only! Same thing goes for TrackedDictionary

        public void Undo()
        {
            this.Tracker.Undo();
        }

        public void Redo()
        {
            this.Tracker.Redo();
        }

        public void AddRestorePoint(string name)
        {
            this.Tracker.AddNamedRestorePoint(name);
        }

        public void UndoToRestorePoint(string name)
        {
            this.Tracker.RestoreNamedRestorePoint(name);
        }
    }
}