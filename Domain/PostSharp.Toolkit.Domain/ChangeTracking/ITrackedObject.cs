using PostSharp.Constraints;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: Need a way to quickly identify tracked aggregate roots

    //TODO: Need to hide / change this interface; Undo/Redo are going to operate on the tracker and some object, not necessarily this one!
    [Internal]
    public interface ITrackedObject : ITrackable
    {
        IObjectTracker Tracker { get; } // TODO make set internal

        [NoAutomaticChangeTrackingOperation]
        void SetTracker(IObjectTracker tracker);

        ////TODO: All operations below should definitly be removed from this interface; SetTracker as well, if possible
        ////(Other option: get rid of the interface, introduce field, compile field getter / setter)


        //int OperationCount { get; }

        //[NoAutomaticChangeTrackingOperation]
        //void Undo();

        //[NoAutomaticChangeTrackingOperation]
        //void Redo();

        //[NoAutomaticChangeTrackingOperation]
        //void AddRestorePoint(string name);

        //[NoAutomaticChangeTrackingOperation]
        //void UndoToRestorePoint( string name );
    }
}