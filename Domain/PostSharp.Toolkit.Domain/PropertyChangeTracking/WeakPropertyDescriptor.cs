#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Runtime.CompilerServices;

namespace PostSharp.Toolkit.Domain.PropertyChangeTracking
{
    internal class WeakPropertyDescriptor : IEquatable<WeakPropertyDescriptor>
    {
        private readonly int hashCode;

        public WeakPropertyDescriptor( object instance, string propertyName )
        {
            this.Instance = new WeakReference( instance );
            this.PropertyPath = propertyName;
            this.Processed = false;

            //Need to calculate hash code here, so that it does not change during object's lifetime:
            this.hashCode = ((RuntimeHelpers.GetHashCode( instance ) * 397) ^ propertyName.GetHashCode());
        }

        public WeakReference Instance { get; private set; }

        public string PropertyPath { get; private set; }

        public bool Processed { get; set; }

        public bool Equals( WeakPropertyDescriptor other )
        {
            if ( other == null )
            {
                return false;
            }

            //Grab the instances so that their IsAlive state remains stable for the duration of the method
            object thisInstance = this.Instance.Target;
            object otherInstance = other.Instance.Target;

            //There's a risk instances are both dead by now and we have no good way to know whether we stored the same instance.
            //For now we're relying on hash code only.
            //If the final INPC algorithm turns out to depend on this equality, we will need to implement ObjectIDGenerator generator based on ConditionalWeakTable and store the IDs instead of hashes
            //(except this may be slow: ConditionalWeakTable does a lot of locking)
            //For now there is no problem since we use WeakPropertyDescriptor only in List<T> and dead instances are not in scope of interest. Problem will occure when we would want to use Dictionary or HashSet

            return this.hashCode == other.hashCode && ReferenceEquals( thisInstance, otherInstance ) && this.PropertyPath == other.PropertyPath;
        }

        public override int GetHashCode()
        {
            return this.hashCode;
        }

        public override bool Equals( object obj )
        {
            WeakPropertyDescriptor other = obj as WeakPropertyDescriptor;

            return this.Equals( other );
        }
    }
}