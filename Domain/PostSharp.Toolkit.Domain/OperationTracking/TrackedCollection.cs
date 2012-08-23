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

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    [ChunkManagement]
    public class TrackedCollection<T> : INotifyCollectionChanged, IList<T>, IList, ITrackedObject
    {
        private readonly ObservableCollection<T> innerCollection;

        private void Initialize()
        {
            this.Tracker = new SingleObjectTracker(this);
        }

        public TrackedCollection()
        {
            Initialize();
            innerCollection = new ObservableCollection<T>();
        }

        public TrackedCollection(List<T> list)
        {
            Initialize();
            innerCollection = new ObservableCollection<T>(list);
        }

        public TrackedCollection(IEnumerable<T> collection)
        {
            Initialize();
            innerCollection = new ObservableCollection<T>(collection);
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
            this.Tracker.AddOperationToChunk( new DelegateOperation<TrackedCollection<T>>(this, c => c.Remove( item ), c => c.Add( item )) );
            this.innerCollection.Add(item);
        }

        int IList.Add(object value)
        {
            this.Tracker.AddOperationToChunk(new DelegateOperation<TrackedCollection<T>>(this, c => ((IList)c).Remove(value), c => ((IList)c).Add(value)));
            return ((IList)this.innerCollection).Add(value);
        }

        bool IList.Contains(object value)
        {
            return ((IList)this.innerCollection).Contains(value);
        }

        //TODO undo !!!
        public void Clear()
        {
            this.innerCollection.Clear();
        }

        int IList.IndexOf(object value)
        {
            return ((IList)this.innerCollection).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            this.Tracker.AddOperationToChunk(new DelegateOperation<TrackedCollection<T>>(this, c => ((IList)c).RemoveAt(index), c => ((IList)c).Insert(index, value)));
            ((IList)this.innerCollection).Insert(index, value);
        }

        void IList.Remove(object value)
        {
            this.Tracker.AddOperationToChunk(new DelegateOperation<TrackedCollection<T>>(this, c => ((IList)c).Add(value), c => ((IList)c).Remove(value)));
            ((IList)this.innerCollection).Remove(value);
        }

        public void RemoveAt(int index)
        {
            object value = ((IList)this)[index];
            this.Tracker.AddOperationToChunk(new DelegateOperation<TrackedCollection<T>>(this, c => ((IList)c).RemoveAt(index), c => ((IList)c).Insert(index, value)));
            ((IList)this.innerCollection).RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return innerCollection[index];
            }
            set
            {
                object newValue = value;
                object oldValue = ((IList)innerCollection)[index];
                this.Tracker.AddOperationToChunk(new DelegateOperation<TrackedCollection<T>>(this, c => ((IList)c)[index] = oldValue, c => ((IList)c)[index] = newValue));
                ((IList)innerCollection)[index] = value;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return ((IList)innerCollection).IsReadOnly;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return ((IList)innerCollection).IsReadOnly;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return ((IList)innerCollection).IsFixedSize;
            }
        }

        public bool Contains(T item)
        {
            return innerCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            innerCollection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return innerCollection.Remove(item);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)innerCollection).CopyTo(array, index);
        }

        public int Count
        {
            get
            {
                return innerCollection.Count;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((ICollection)innerCollection).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((ICollection)innerCollection).IsSynchronized;
            }
        }

        public int IndexOf(T item)
        {
            return this.innerCollection.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.Tracker.AddOperationToChunk(new DelegateOperation<TrackedCollection<T>>(this, c => c.RemoveAt(index), c => c.Insert(index, item)));
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
                T oldValue = innerCollection[index];
                this.Tracker.AddOperationToChunk(new DelegateOperation<TrackedCollection<T>>(this, c => c[index] = oldValue, c => c[index] = newValue));
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
                return this.Tracker.OperationCount;
            }
        }

        public void Undo()
        {
            this.Tracker.Undo();
        }

        public void Redo()
        {
            this.Tracker.Redo();
        }

        public void AddNamedRestorePoint(string name)
        {
            this.Tracker.AddNamedRestorePoint(name);
        }

        public void RestoreNamedRestorePoint(string name)
        {
            this.Tracker.RestoreNamedRestorePoint(name);
        }
    }
}