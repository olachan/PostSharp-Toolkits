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
                    default:
                        return null;
                }
            }
        }
    }
}