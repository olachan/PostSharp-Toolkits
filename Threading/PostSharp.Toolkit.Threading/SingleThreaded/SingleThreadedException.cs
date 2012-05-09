using System;

namespace PostSharp.Toolkit.Threading.SingleThreaded
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SingleThreadedException : Exception
    {
        public SingleThreadedException()
            : base("An attempt was made to simultaneously access a single-threaded method from multiple threads.")
        {
            
        }
    }
}
