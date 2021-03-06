﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections;
using System.Collections.Generic;

using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain
{
    [ImplicitOperationManagement]
    public class TrackedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, ITrackedObject
    {
        private readonly Dictionary<TKey, TValue> innerCollection;

        private CollectionTrackingStrategy collectionTrackingStrategy;

        private void Initialize(CollectionTrackingStrategy collectionTrackingStrategy)
        {
            this.collectionTrackingStrategy = collectionTrackingStrategy;
            this.AggregateTracker = new AggregateTracker(this, false);
        }

        public TrackedDictionary(CollectionTrackingStrategy collectionTrackingStrategy)
            : this(0, null, collectionTrackingStrategy)
        {
        }

        public TrackedDictionary(int capacity, CollectionTrackingStrategy collectionTrackingStrategy)
            : this(capacity, null, collectionTrackingStrategy)
        {
        }

        public TrackedDictionary(IEqualityComparer<TKey> comparer, CollectionTrackingStrategy collectionTrackingStrategy)
            : this(0, comparer, collectionTrackingStrategy)
        {
        }

        public TrackedDictionary(int capacity, IEqualityComparer<TKey> comparer, CollectionTrackingStrategy collectionTrackingStrategy)
        {
            this.Initialize(collectionTrackingStrategy);
            this.innerCollection = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        public TrackedDictionary(IDictionary<TKey, TValue> dictionary, CollectionTrackingStrategy collectionTrackingStrategy)
            : this(dictionary, null, collectionTrackingStrategy)
        {
        }

        public TrackedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer, CollectionTrackingStrategy collectionTrackingStrategy)
            : this(dictionary != null ? dictionary.Count : 0, comparer, collectionTrackingStrategy)
        {
            foreach (KeyValuePair<TKey, TValue> keyValuePair in dictionary)
                this.innerCollection.Add(keyValuePair.Key, keyValuePair.Value);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)this.innerCollection).GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            object oldValue = ((IDictionary)this.innerCollection)[key];
            this.AggregateTracker.AddToCurrentOperation(
                new DelegateOperation(
                () => ((IDictionary)this).Add(key, oldValue),
                () => ((IDictionary)this).Remove(key)));
            
            this.DetachFromAggregate( oldValue );

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

                this.AggregateTracker.AddToCurrentOperation(
                    new DelegateOperation(
                    () =>
                    {
                        if (revertOldValue) ((IDictionary)this)[key] = oldValue;
                        else ((IDictionary)this).Remove(key);
                    },
                    () => ((IDictionary)this)[key] = value));

                this.AttachToAggregate(value);
                this.DetachFromAggregate(oldValue);

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
            this.AggregateTracker.AddToCurrentOperation(
                new DelegateOperation(
                () => ((IDictionary)this).Remove(key),
                () => ((IDictionary)this).Add(key, value)));

            this.AttachToAggregate(value);

            ((IDictionary)this.innerCollection).Add(key, value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            this.AggregateTracker.AddToCurrentOperation(
              new DelegateOperation(
              () => ((ICollection<KeyValuePair<TKey, TValue>>)this).Remove(item),
              () => ((ICollection<KeyValuePair<TKey, TValue>>)this).Add(item)));

            this.AttachToAggregate(item);

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

            this.AggregateTracker.AddToCurrentOperation(
               new DelegateOperation(
               () =>
               {
                   foreach (KeyValuePair<TKey, TValue> pair in copy)
                   {
                       this.Add(pair.Key, pair.Value);
                   }
               },
               () => this.Clear()));

            foreach ( KeyValuePair<TKey, TValue> keyValuePair in copy )
            {
                this.DetachFromAggregate( keyValuePair.Value );
            }

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
            this.AggregateTracker.AddToCurrentOperation(
             new DelegateOperation(
             () => ((ICollection<KeyValuePair<TKey, TValue>>)this).Add(item),
             () => ((ICollection<KeyValuePair<TKey, TValue>>)this).Remove(item)));

            this.DetachFromAggregate(item);

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
            ((ICollection)this.innerCollection).CopyTo(array, index);
        }

        public int Count
        {
            get
            {
                return this.innerCollection.Count;
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

        public bool ContainsKey(TKey key)
        {
            return this.innerCollection.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            this.AggregateTracker.AddToCurrentOperation(
              new DelegateOperation(
              () => this.Remove(key),
              () => this.Add(key, value)));

            this.AttachToAggregate(value);

            this.innerCollection.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            TValue oldValue = this.innerCollection[key];

            this.AggregateTracker.AddToCurrentOperation(
                new DelegateOperation(
                () => this.Add(key, oldValue),
                () => this.Remove(key)));

            this.DetachFromAggregate(oldValue);

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

                this.AggregateTracker.AddToCurrentOperation(
                    new DelegateOperation(
                    () =>
                    {
                        if (revertOldValue) this[key] = oldValue;
                        else this.Remove(key);
                    },
                    () => this[key] = value));

                this.AttachToAggregate(value);
                this.DetachFromAggregate(oldValue);

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

        private void AttachToAggregate(object item)
        {
            if (this.collectionTrackingStrategy == CollectionTrackingStrategy.TrackAllContent)
            {
                ((AggregateTracker)this.Tracker).AttachToAggregate(item);
            }
        }

        private void DetachFromAggregate(object item)
        {
            if (this.collectionTrackingStrategy == CollectionTrackingStrategy.TrackAllContent)
            {
                ((AggregateTracker)this.Tracker).DetachFromAggregate(item, false);
            }
        }

        [ChangeTrackingIgnoreOperation]
        public IObjectTracker Tracker
        {
            get
            {
                return this.AggregateTracker;
            }
        }

        [ChangeTrackingIgnoreField]
        internal AggregateTracker AggregateTracker { get; private set; }

        public void SetTracker(IObjectTracker tracker)
        {
            this.AggregateTracker = (AggregateTracker)tracker;
        }

        public bool IsAggregateRoot
        {
            get
            {
                return ReferenceEquals(this.AggregateTracker.AggregateRoot, this);
            }
        }

        public bool IsTracked
        {
            get
            {
                return this.AggregateTracker.IsTracking;
            }
        }

        public int OperationCount
        {
            get
            {
                return this.AggregateTracker.OperationsCount;
            }
        }
    }
}