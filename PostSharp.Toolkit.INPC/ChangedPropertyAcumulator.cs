using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PostSharp.Toolkit.INPC
{
    internal static class ChangedPropertyAcumulator
    {
        private class WeakPropertyDescriptor
        {
            public WeakPropertyDescriptor(object instance, string propertyName)
            {
                this.Instance = new WeakReference( instance );
                this.PropertyName = propertyName;
            }

            public WeakReference Instance { get; set; }

            public string PropertyName { get; set; }

            protected bool Equals( WeakPropertyDescriptor other )
            {
                return other != null && 
                    this.Instance.IsAlive && 
                    other.Instance.IsAlive && 
                    ReferenceEquals( this.Instance, other.Instance) && 
                    this.PropertyName == other.PropertyName;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((this.Instance != null ? this.Instance.GetHashCode() : 0) * 397) ^ (this.PropertyName != null ? this.PropertyName.GetHashCode() : 0);
                }
            }

            public override bool Equals(object obj)
            {
                WeakPropertyDescriptor other = obj as WeakPropertyDescriptor;

                return Equals( other );
            }
        }

        [NonSerialized]
        [ThreadStatic]
        private static Stack<object> stackTrace;

        private static Stack<object> StackTrace
        {
            get
            {
                return stackTrace ?? (stackTrace = new Stack<object>());
            }
        }

        [ThreadStatic]
        private static IList<WeakPropertyDescriptor> changedProperties;

        private static IList<WeakPropertyDescriptor> ChangedProperties
        {
            get
            {
                return changedProperties ?? (changedProperties = new List<WeakPropertyDescriptor>());
            }
        }

        public static void AddProperty(object obj, string propertyName)
        {
            foreach ( WeakPropertyDescriptor weakPropertyDescriptor in ChangedProperties )
            {
                if (weakPropertyDescriptor.Instance.IsAlive && ReferenceEquals( weakPropertyDescriptor.Instance.Target, obj ) && weakPropertyDescriptor.PropertyName == propertyName)
                {
                    return;
                }
            }

            ChangedProperties.Add( new WeakPropertyDescriptor( obj, propertyName ) );
        }

        public static void PushOnStack(object o)
        {
            StackTrace.Push( o );
        }

        public static bool PopFromStack(object o)
        {
            StackTrace.Pop();
            return StackTrace.Peek() == o;
        }

        public static void RaisePropertyChanged(object instance, bool popFromStack)
        {
            if (popFromStack)
            {
                StackTrace.Pop();
            }

            if (StackTrace.Count > 0 && StackTrace.Peek() == instance)
            {
                return;
            }

            Compact();

            var objectsToRisePropertyChanged = ChangedProperties.Where(w => w.Instance.IsAlive && !StackTrace.Contains(w.Instance.Target)).ToList(); // ChangedObjects.Except(StackTrace).Union(new[] { instance });

            foreach (var w in objectsToRisePropertyChanged)
            {
                ChangedProperties.Remove( w );

                IRaiseNotifyPropertyChanged rpc = w.Instance.Target as IRaiseNotifyPropertyChanged;
                if (rpc != null)
                {
                    rpc.OnPropertyChanged(w.PropertyName);
                }
            }
        }

        private static void Compact()
        {
            var deadObjects = ChangedProperties.Where( w => !w.Instance.IsAlive ).ToList();
            foreach ( WeakPropertyDescriptor weakPropertyDescriptor in deadObjects )
            {
                ChangedProperties.Remove( weakPropertyDescriptor );
            }
        }
    }
}
