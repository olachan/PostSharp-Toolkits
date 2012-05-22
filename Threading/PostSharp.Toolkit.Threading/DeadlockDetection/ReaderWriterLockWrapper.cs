using System.Threading;

namespace PostSharp.Toolkit.Threading.DeadlockDetection
{
    [DeadlockDetectionPolicy.ReaderWriterEnhancements]
    public class ReaderWriterLockInstrumentedWrapper : ReaderWriterLockWrapper
    {
        public override void EnterReadLock()
        {
            base.EnterReadLock();
        }

        public override void EnterWriteLock()
        {
            base.EnterWriteLock();
        }

        public override void ExitReadLock()
        {
            base.ExitReadLock();
        }

        public override void ExitWriteLock()
        {
            base.ExitWriteLock();
        }
    }

    public class ReaderWriterLockWrapper : ReaderWriterLockSlim
    {
        public virtual void EnterReadLock()
        {
            base.EnterReadLock();
        }

        public virtual void EnterWriteLock()
        {
            base.EnterWriteLock();
        }

        public virtual void ExitReadLock()
        {
            base.ExitReadLock();
        }

        public virtual void ExitWriteLock()
        {
            base.ExitWriteLock();
        }
    }
}
