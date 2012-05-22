using System.Threading;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    public interface IThreadAffined
    {
        SynchronizationContext SynchronizationContext { get; set; }
    }
}