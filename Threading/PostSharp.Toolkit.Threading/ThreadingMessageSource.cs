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
                        return "Asynchronous DispatchedMethodAttribute cannot be applied to {0}.{1}. It can only be applied to void methods.";
                    case 2:
                        return "static method {0}.{1} can not be marked ThreadUnsafe.";
                    case 3:
                        return "method {0}.{1} should not be marked ThreadUnsafe when ThreadUnsafePolicy is Static";
                    default:
                        return null;
                }
            }
        }
    }
}
