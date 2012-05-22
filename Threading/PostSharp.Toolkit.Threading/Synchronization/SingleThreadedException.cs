using System;

namespace PostSharp.Toolkit.Threading.Synchronization
{
    /// <summary>
    /// Exception thrown when an attempt to simultaneously access a single-threaded method is detected.
    /// </summary>
    public class SingleThreadedException : Exception
    {
        public SingleThreadedException(string msg)
            : base(msg)
        { }
    }
}
