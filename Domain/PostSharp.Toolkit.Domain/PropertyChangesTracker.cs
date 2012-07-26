#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Domain
{
    internal static class PropertyChangesTracker
    {
        private static readonly ThreadLocal<ChangedPropertiesAccumulator> propertyChangesAcumulator =
            new ThreadLocal<ChangedPropertiesAccumulator>(() => new ChangedPropertiesAccumulator());
        private static readonly ThreadLocal<ChangedPropertiesAccumulator> childPropertyChangesAcumulator =
            new ThreadLocal<ChangedPropertiesAccumulator>(() => new ChangedPropertiesAccumulator());

        private static readonly ThreadLocal<StackContext> stackTrace = new ThreadLocal<StackContext>(() => new StackContext());

        private static ChangedPropertiesAccumulator PropertyChangesAccumulator
        {
            get
            {
                return propertyChangesAcumulator.Value;
            }
        }

        private static ChangedPropertiesAccumulator ChildPropertyChangesAccumulator
        {
            get
            {
                return childPropertyChangesAcumulator.Value;
            }
        }

        private static StackContext StackContext
        {
            get
            {
                return stackTrace.Value;
            }
        }

        public static void PushOnStack(object o)
        {
            StackContext.PushOnStack(o);
        }

        public static object PopFromStack()
        {
            return StackContext.Pop();
        }

        public static object StackPeek()
        {
            return StackContext.Count == 0 ? null : StackContext.Peek();
        }

        public static void RaisePropertyChangedIfNeeded(LocationInterceptionArgs args)
        {
            if (StackPeek() != args.Instance)
            {
                RaisePropertyChanged();
            }
        }

        public static void StoreChangedProperties(object instance, List<string> properties)
        {
            // StoreChangedChildProperties(instance, properties);
            PropertyChangesAccumulator.AddProperties(instance, properties);
        }

        public static void StoreChangedChildProperty(object instance, string propertyPath)
        {
            ChildPropertyChangesAccumulator.AddProperty(instance, propertyPath);
        }

        public static void StoreChangedChildProperties(object instance, List<string> propertyPaths)
        {
            ChildPropertyChangesAccumulator.AddProperties(instance, propertyPaths);
        }

        public static void RaisePropertyChanged()
        {
            RaiseChildPropertyChanged();
            RaisePropertyChangesInternal(propertyChangesAcumulator.Value, false);
        }

        public static void RaisePropertyChanged(object instance)
        {
            RaiseChildPropertyChanged();
            RaisePropertyChangesInternal(propertyChangesAcumulator.Value, false, instance);
        }

        public static void RaiseChildPropertyChanged()
        {
            RaisePropertyChangesInternal(childPropertyChangesAcumulator.Value, true);
        }
        
        private static void RaisePropertyChangesInternal(ChangedPropertiesAccumulator accumulator, bool raiseChildPropertyChanges, object instance = null)
        {
            accumulator.Compact();

            List<WeakPropertyDescriptor> objectsToRaisePropertyChanged;

            int loopCount = 0;

            do
            {
                objectsToRaisePropertyChanged = instance == null ? 
                    accumulator.Where(w => w.Instance.IsAlive && !stackTrace.Value.Contains(w.Instance.Target)).ToList() : 
                    accumulator.Where(w => w.Instance.IsAlive && ReferenceEquals(w.Instance.Target, instance)).ToList();

                foreach (WeakPropertyDescriptor w in objectsToRaisePropertyChanged)
                {
                    //INPC handler may raise INPC again and process some of our events;
                    //we're working on accumulator copy, so only way to know it is to have a flag on the descriptor
                    if (w.Processed)
                    {
                        continue;
                    }

                    w.Processed = true;
                    accumulator.Remove(w);
                    object cpc = w.Instance.Target;

                    if (cpc != null) //Target may not be alive any more
                    {
                        if (raiseChildPropertyChanges)
                        {
                            NotifyPropertyChangedAccessor.RaiseChildPropertyChanged( cpc, w.PropertyPath );
                        }
                        else
                        {
                            NotifyPropertyChangedAccessor.RaisePropertyChanged( cpc, w.PropertyPath );
                        }
                    }
                }
                //Notifications may cause generation of new notifications, continue until there is nothing left to raise
            }
            while (objectsToRaisePropertyChanged.Count > 0 && loopCount++ <= 12);

            //TODO: Verify the loop above will always stop.
            //If there's a risk it won't, call RaiseChildPropertyChanged from NPCAttribute.ChildPropertyChangedEventHandler to bubble up CNPC notifications
            //(this may lead to a lot of unneccessary recursion, however, and a lot of continuations on w.Processed check above)
        }
    }
}