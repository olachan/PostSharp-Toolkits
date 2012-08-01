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
                RaisePropertyChanged(args.Instance);
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

        public static void RaisePropertyChanged(object source)
        {
            RaiseChildPropertyChanged(source);
            RaisePropertyChangesInternal(
                propertyChangesAcumulator.Value, 
                false,
                w => w.Instance.IsAlive && (!stackTrace.Value.Contains(w.Instance.Target) || ReferenceEquals(w.Instance.Target, source)));
        }

        public static void RaisePropertyChangedOnlyOnSpecifiedInstance(object instance)
        {
            RaiseChildPropertyChanged(instance);
            RaisePropertyChangesInternal(
                propertyChangesAcumulator.Value, 
                false, 
                w => w.Instance.IsAlive && ReferenceEquals(w.Instance.Target, instance));
        }

        public static void RaisePropertyChangedIncludingCurrentObject()
        {
            RaisePropertyChanged( StackPeek() );
        }

        public static void RaiseChildPropertyChanged(object source)
        {
            RaisePropertyChangesInternal(
                childPropertyChangesAcumulator.Value,
                true,
                w => w.Instance.IsAlive);//&& (!stackTrace.Value.Contains(w.Instance.Target) || ReferenceEquals(w.Instance.Target, source)));
        }
        
        private static void RaisePropertyChangesInternal(
            ChangedPropertiesAccumulator accumulator, 
            bool raiseChildPropertyChanges, 
            Func<WeakPropertyDescriptor, bool> propertySelectorPredicate )
        {
            accumulator.Compact();

            List<WeakPropertyDescriptor> objectsToRaisePropertyChanged;

            int loopCount = 0;

            Dictionary<WeakPropertyDescriptor, int> raiseCounts = accumulator.ToDictionary( w => w, w => 0 );

            do
            {
                objectsToRaisePropertyChanged = accumulator.Where(propertySelectorPredicate).ToList();

                foreach (WeakPropertyDescriptor w in objectsToRaisePropertyChanged)
                {
                    int raiseCount = raiseCounts.GetOrCreate(w, () => 0);
                    
                    //INPC handler may raise INPC again and process some of our events;
                    //we're working on accumulator copy, so only way to know it is to have a flag on the descriptor

                    if (w.Processed || raiseChildPropertyChanges ? raiseCount > 0 : raiseCount > 1)
                    {
                        w.Processed = true;
                        continue;
                    }

                    w.Processed = true;
                    accumulator.Remove(w);
                    object cpc = w.Instance.Target;

                    if (cpc != null) //Target may not be alive any more
                    {
                        raiseCounts[w]++;

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
            while (objectsToRaisePropertyChanged.Count > 0 && ++loopCount <= 32);

            //TODO: Verify the loop above will always stop.
            //If there's a risk it won't, call RaiseChildPropertyChanged from NPCAttribute.ChildPropertyChangedEventHandler to bubble up CNPC notifications
            //(this may lead to a lot of unneccessary recursion, however, and a lot of continuations on w.Processed check above)

            // if we encounter infinite loop generate meaningful exception for bug report.
            if (loopCount == 32)
            {
                throw new NotifyPropertyChangedAlgorithmInfiniteLoop( "Encountered infinite loop while raising PropertyChanged events" );
            }
        }
    }

    public class NotifyPropertyChangedAlgorithmInfiniteLoop : Exception
    {
        public NotifyPropertyChangedAlgorithmInfiniteLoop()
        {
        }

        public NotifyPropertyChangedAlgorithmInfiniteLoop( string message )
            : base( message )
        {
        }

        public NotifyPropertyChangedAlgorithmInfiniteLoop( string message, Exception innerException )
            : base( message, innerException )
        {
        }
    }
}