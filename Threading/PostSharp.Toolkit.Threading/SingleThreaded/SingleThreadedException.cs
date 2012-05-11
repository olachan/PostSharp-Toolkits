using System;

namespace PostSharp.Toolkit.Threading.SingleThreaded
{
    /// <summary>
    /// Exception thrown when an attempt to simultaneously access a single-threaded method is detected.
    /// </summary>
    public class SingleThreadedException : Exception
    {
        public SingleThreadedException()
            : base("An attempt was made to simultaneously access a single-threaded method from multiple threads.")
        {
            
        }
    }
}
