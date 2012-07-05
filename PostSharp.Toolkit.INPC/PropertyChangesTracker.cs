using System;
using System.Linq;
using System.Threading;

namespace PostSharp.Toolkit.INPC
{
    internal static class PropertyChangesTracker
    {
        private static readonly ThreadLocal<ChangedPropertiesAccumulator> changedPropertiesAcumulator =
                                        new ThreadLocal<ChangedPropertiesAccumulator>(() => new ChangedPropertiesAccumulator());

        private static readonly ThreadLocal<StackContext> stackTrace =
                                        new ThreadLocal<StackContext>(() => new StackContext());

        public static ChangedPropertiesAccumulator Accumulator
        {
            get { return changedPropertiesAcumulator.Value; }
        }

        public static StackContext StackContext
        {
            get { return stackTrace.Value; }
        }


        public static void RaisePropertyChanged(object instance, bool popFromStack)
        {
            ChangedPropertiesAccumulator accumulator = changedPropertiesAcumulator.Value;
            if (popFromStack)
            {
                stackTrace.Value.Pop( );
            }

            if (stackTrace.Value.Count > 0 && stackTrace.Value.Peek() == instance)
            {
                return;
            }

            accumulator.Compact();

            var objectsToRaisePropertyChanged =
                accumulator.Where(w => w.Instance.IsAlive &&
                                                        !stackTrace.Value.Contains(w.Instance.Target)).ToList();
                                                        // ChangedObjects.Except(StackContext).Union(new[] { instance });

            foreach (var w in objectsToRaisePropertyChanged)
            {
                accumulator.Remove(w);

                IRaiseNotifyPropertyChanged rpc = w.Instance.Target as IRaiseNotifyPropertyChanged;
                if (rpc != null)
                {
                    rpc.OnPropertyChanged(w.PropertyName);
                }
            }
        }
    }
}