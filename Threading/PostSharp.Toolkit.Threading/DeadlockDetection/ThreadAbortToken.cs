namespace PostSharp.Toolkit.Threading.DeadlockDetection
{
    internal class ThreadAbortToken
    {
        private readonly string message;

        public ThreadAbortToken(string message)
        {
            this.message = message;
        }

        public string Message
        {
            get
            {
                return this.message;
            }
        }
    }
}
