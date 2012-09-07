using PostSharp.Constraints;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [Internal]
    public interface ITrackedObject
    {
        IObjectTracker Tracker { get; }

        [ChangeTrackingIgnoreOperation]
        void SetTracker(IObjectTracker tracker);

        bool IsAggregateRoot { get; }

        bool IsTracked { get; }
    }
}