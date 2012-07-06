using PostSharp.Extensibility;

namespace PostSharp.Toolkit.INPC
{
    internal static class InpcMessageSource
    {
        public static MessageSource Instance = new MessageSource("PostSharp.Toolkit.INPC", new InpcMessageDispenser());

        private class InpcMessageDispenser : MessageDispenser
        {
            public InpcMessageDispenser()
                : base("INPC")
            {
            }

            protected override string GetMessage(int number)
            {
                switch (number)
                {
                    case 1:
                        return
                            "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. "+
                            "Method {1} contains direct access to a field of another class."+
                            "Use attribute TODO or TODO to TODO.";
                    case 2:
                        return
                            "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " +
                            "Method {1} contains call to non void (ref/out param) method of another class." +
                            "Use attribute TODO or TODO to TODO.";
                    case 3:
                        return
                            "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " +
                            "Method {1} contains delegate call." +
                            "Use attribute TODO or TODO to TODO.";
                    case 4:
                        return "Aspect NotifyPropertyChangedPolicy must be added to the current assembly only.";
                    default:
                        return null;
                }
            }
        }
    }
}