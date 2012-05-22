using System;
using System.Linq;
using System.Collections.Generic;

namespace PostSharp.Toolkit.Threading.DeadlockDetection
{
    internal class WeakHashSet
    {
        private readonly Dictionary<int, List<WeakReference>> dictionary;

        public WeakHashSet()
        {
            this.dictionary = new Dictionary<int, List<WeakReference>>();
            this.Count = 0;
        }

        public int Count { get; private set; }

        public bool Add(object item)
        {
            int key = item.GetHashCode();
            List<WeakReference> list;
            if (!this.dictionary.TryGetValue(key, out list))
            {
                list = new List<WeakReference>();
                this.dictionary.Add(key, list);
            }

            if (list.FirstOrDefault(w => w.IsAlive && Equals(w.Target, item)) == null)
            {
                var wr = new WeakReference(item);
                list.Add(wr);
                this.Count++;
                return true;
            }

            return false;
        }

        public bool Remove(object item)
        {
            int key = item.GetHashCode();
            List<WeakReference> list;
            if (!this.dictionary.TryGetValue(key, out list))
            {
                return false;
            }

            int removed = list.RemoveAll(w => w.IsAlive && Equals(w.Target, item));
            this.Count -= removed;

            return removed > 0;
        }

        public bool Contains(object item)
        {
            int key = item.GetHashCode();
            List<WeakReference> list;
            if (!this.dictionary.TryGetValue(key, out list))
            {
                return false;
            }

            if (list.FirstOrDefault(w => w.IsAlive && Equals(w.Target, item)) != null)
            {
                return true;
            }

            return false;
        }

        public int ClearNotAlive()
        {
            int removed = this.dictionary.Values.Sum(list => list.RemoveAll(w => !w.IsAlive));
            this.Count -= removed;
            return removed;
        }
    }
}