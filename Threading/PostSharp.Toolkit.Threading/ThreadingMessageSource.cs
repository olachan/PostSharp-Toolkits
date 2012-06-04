using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading
{
    internal static class ThreadingMessageSource
    {
        public static MessageSource Instance = new MessageSource("PostSharp.Toolkit.Threading", new ThreadingMessageDispenser());

        private class ThreadingMessageDispenser : MessageDispenser
        {
            public ThreadingMessageDispenser()
                : base("THR")
            {
            }

            protected override string GetMessage(int number)
            {
                switch (number)
                {
                    case 1:
                        return "Asynchronous DispatchedMethodAttribute cannot be applied to {0}.{1}. It can only be applied to void methods with no ref or out parameters.";
                    case 2:
                        return "static method {0}.{1} can not be marked ThreadUnsafe.";
                    case 3:
                        return "method {0}.{1} should not be marked ThreadUnsafe when ThreadUnsafePolicy is Static";
                    case 4:
                        return "ActorAttribute can not be  to type {0}. It can only be applied to types derived from Actor class.";
                    case 5:
                        return "ActorAttribute can not be applied to type {0} because it contains public field {0}.{1}";
                    case 6:
                        return "BackgroundMethodAttribute cannot be applied to {0}.{1}. It can only be applied to void methods without out/ref parameters.";
                    case 7:
                        return "ReaderWriterSynchronizedAttribute cannot be applied to type {0} becouse it contains public field {0}.{1} not marked as ThreadSafe.";
                    case 8:
                        return "ThreadUnsafeObject cannot be applied to type {0} becouse it contains public field {0}.{1} not marked as ThreadSafe.";
                    case 9:
                        return "ActorAttribute cannot be applied to type {0} becouse it contains method {0}.{1} not returning void or containing out/ref parameters.";
                    case 10:
                        return "Aspect DeadlockDetectionPolicy must be added to the current assembly only.";
                    default:
                        return null;
                }
            }
        }
    }
}
