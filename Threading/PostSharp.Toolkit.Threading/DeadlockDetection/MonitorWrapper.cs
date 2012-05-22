using System.Threading;

namespace PostSharp.Toolkit.Threading.DeadlockDetection
{
    [DeadlockDetectionPolicy.MonitorEnhancements]
    internal static class MonitorWrapper
    {
        public static void Enter(object lockObject)
        {
            Monitor.Enter(lockObject);
        }

        public static void Exit(object lockObject)
        {
            Monitor.Exit(lockObject);
        }
    }
}
