﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Domain.PropertyChangeTracking
{
    internal sealed class ChangedPropertiesAccumulator : IEnumerable<WeakPropertyDescriptor>
    {
        private readonly List<WeakPropertyDescriptor> changedProperties = new List<WeakPropertyDescriptor>();

        public void AddProperty(object obj, string propertyName, bool resetProcessed = true, bool matchPrefix = false)
        {
            WeakPropertyDescriptor propertyToRemove = null;
            foreach (WeakPropertyDescriptor weakPropertyDescriptor in this.changedProperties)
            {
                if (!weakPropertyDescriptor.Instance.IsAlive || 
                    !ReferenceEquals(weakPropertyDescriptor.Instance.Target, obj))
                {
                    continue;
                }

                if (!matchPrefix && weakPropertyDescriptor.PropertyPath != propertyName)
                {
                    continue;
                }

                if (!matchPrefix || propertyName.StartsWith(weakPropertyDescriptor.PropertyPath))
                {
                    if (weakPropertyDescriptor.Processed && resetProcessed)
                    {
                        weakPropertyDescriptor.Processed = false;
                    }

                    return;
                }

                if (weakPropertyDescriptor.PropertyPath.StartsWith( propertyName ) && !weakPropertyDescriptor.Processed)
                {
                    propertyToRemove = weakPropertyDescriptor;
                }
            }

            if (propertyToRemove != null)
            {
                this.changedProperties.Remove( propertyToRemove );
            }

            this.changedProperties.Add(new WeakPropertyDescriptor(obj, propertyName));
        }

        public void AddProperties(object obj, IEnumerable<string> propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                this.AddProperty(obj, propertyName);
            }
        }

        public void Remove(WeakPropertyDescriptor propertyDescriptor)
        {
            this.changedProperties.Remove(propertyDescriptor);
        }

        public void Compact()
        {
            List<WeakPropertyDescriptor> deadObjects = this.changedProperties.Where(w => !w.Instance.IsAlive).ToList();
            foreach (WeakPropertyDescriptor weakPropertyDescriptor in deadObjects)
            {
                this.changedProperties.Remove(weakPropertyDescriptor);
            }
        }

        public IEnumerator<WeakPropertyDescriptor> GetEnumerator()
        {
            return this.changedProperties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}