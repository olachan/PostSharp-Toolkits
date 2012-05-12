using System.Threading;

namespace PostSharp.Toolkit.Threading.ThreadAffined
{
    public interface IThreadAffined
    {
        SynchronizationContext SynchronizationContext { get; set; }
    }
}