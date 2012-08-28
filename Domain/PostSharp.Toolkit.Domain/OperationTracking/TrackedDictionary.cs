#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections;
using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    [ChunkManagement]
    public class TrackedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, ITrackedObject
    {
        private readonly Dictionary<TKey, TValue> innerCollection;

        private void Initialize()
        {
            this.Tracker = new SingleObjectTracker(this);
        }

        public TrackedDictionary()
            : this(0, (IEqualityComparer<TKey>)null)
        {
        }

        public TrackedDictionary(int capacity)
            : this(capacity, (IEqualityComparer<TKey>)null)
        {
        }

        public TrackedDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        public TrackedDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.Initialize();
            this.innerCollection = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        public TrackedDictionary(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, (IEqualityComparer<TKey>)null)
        {
        }

        public TrackedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
            : this(dictionary != null ? dictionary.Count : 0, comparer)
        {
            foreach (KeyValuePair<TKey, TValue> keyValuePair in (IEnumerable<KeyValuePair<TKey, TValue>>)dictionary)
                this.innerCollection.Add(keyValuePair.Key, keyValuePair.Value);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)this.innerCollection).GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            object oldValue = ((IDictionary)this.innerCollection)[key];
            this.Tracker.AddOperationToChunk(
                new DelegateOperation<TrackedDictionary<TKey, TValue>>(
                this,
                d => ((IDictionary)d).Add(key, oldValue),
                d => ((IDictionary)d).Remove(key)));

            ((IDictionary)this.innerCollection).Remove(key);
        }

        object IDictionary.this[object key]
        {
            get
            {
                return ((IDictionary)this.innerCollection)[key];
            }
            set
            {
                object oldValue = null;
                bool revertOldValue = false;

                if (((IDictionary)this.innerCollection).Contains(key))
                {
                    revertOldValue = true;
                    oldValue = ((IDictionary)this.innerCollection)[key];
                }

                this.Tracker.AddOperationToChunk(
                    new DelegateOperation<TrackedDictionary<TKey, TValue>>(
                    this,
                    d =>
                    {
                        if (revertOldValue) ((IDictionary)d)[key] = oldValue;
                        else ((IDictionary)d).Remove(key);
                    },
                    d => ((IDictionary)d)[key] = value));

                ((IDictionary)this.innerCollection)[key] = value;
            }
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return this.innerCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        void IDictionary.Add(object key, object value)
        {
            this.Tracker.AddOperationToChunk(
                new DelegateOperation<TrackedDictionary<TKey, TValue>>(
                this,
                d => ((IDictionary)d).Remove(key),
                d => ((IDictionary)d).Add(key, value)));

            ((IDictionary)this.innerCollection).Add(key, value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            this.Tracker.AddOperationToChunk(
              new DelegateOperation<TrackedDictionary<TKey, TValue>>(
              this,
              d => ((ICollection<KeyValuePair<TKey, TValue>>)d).Remove(item),
              d => ((ICollection<KeyValuePair<TKey, TValue>>)d).Add(item)));

            ((ICollection<KeyValuePair<TKey, TValue>>)this.innerCollection).Add(item);
        }

        bool IDictionary.Contains(object key)
        {
            return ((IDictionary)this.innerCollection).Contains(key);
        }

        public void Clear()
        {
            KeyValuePair<TKey, TValue>[] copy = new KeyValuePair<TKey, TValue>[this.innerCollection.Count];
            ((ICollection<KeyValuePair<TKey, TValue>>)this.innerCollection).CopyTo(copy, 0);

            this.Tracker.AddOperationToChunk(
               new DelegateOperation<TrackedDictionary<TKey, TValue>>(
               this,
               d =>
                   {
                       foreach (KeyValuePair<TKey, TValue> pair in copy)
                       {
                           d.Add(pair.Key, pair.Value);
                       }
                   },
               d => d.Clear()));

            this.innerCollection.Clear();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)this.innerCollection).GetEnumerator();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)this.innerCollection).Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)this.innerCollection).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            this.Tracker.AddOperationToChunk(
             new DelegateOperation<TrackedDictionary<TKey, TValue>>(
             this,
             d => ((ICollection<KeyValuePair<TKey, TValue>>)d).Add(item),
             d => ((ICollection<KeyValuePair<TKey, TValue>>)d).Remove(item)));

            return ((ICollection<KeyValuePair<TKey, TValue>>)this.innerCollection).Remove(item);
        }

        ICollection IDictionary.Values
        {
            get
            {
                return this.innerCollection.Values;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                return ((IDictionary)this.innerCollection).IsReadOnly;
            }
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                return ((IDictionary)this.innerCollection).IsFixedSize;
            }
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

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)this.innerCollection).IsReadOnly;
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

        public bool ContainsKey(TKey key)
        {
            return this.innerCollection.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            this.Tracker.AddOperationToChunk(
              new DelegateOperation<TrackedDictionary<TKey, TValue>>(
              this,
              d => d.Remove(key),
              d => d.Add(key, value)));

            this.innerCollection.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            TValue oldValue = this.innerCollection[key];

            this.Tracker.AddOperationToChunk(
                new DelegateOperation<TrackedDictionary<TKey, TValue>>(
                this,
                d => d.Add(key, oldValue),
                d => d.Remove(key)));

            return this.innerCollection.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.innerCollection.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.innerCollection[key];
            }
            set
            {
                TValue oldValue = default(TValue);
                bool revertOldValue = false;

                if (this.innerCollection.ContainsKey(key))
                {
                    revertOldValue = true;
                    oldValue = this.innerCollection[key];
                }

                this.Tracker.AddOperationToChunk(
                    new DelegateOperation<TrackedDictionary<TKey, TValue>>(
                    this,
                    d =>
                    {
                        if (revertOldValue) d[key] = oldValue;
                        else d.Remove(key);
                    },
                    d => d[key] = value));

                this.innerCollection[key] = value;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return this.innerCollection.Keys;
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return this.innerCollection.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return this.innerCollection.Values;
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