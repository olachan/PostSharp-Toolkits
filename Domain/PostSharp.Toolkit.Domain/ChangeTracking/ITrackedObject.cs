using PostSharp.Constraints;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [Internal]
    public interface ITrackedObject
    {
        IObjectTracker Tracker { get; }

        [NoAutomaticChangeTrackingOperation]
        void SetTracker(IObjectTracker tracker);

        bool IsAggregateRoot { get; }

        bool IsTracked { get; }
    }
}