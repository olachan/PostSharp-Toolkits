namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public interface IObjectTracker : ITracker, ITrackable
    {
        ITracker ParentTracker { get; set; } //TODO: internal interface?

        void StartChunk();

        void EndChunk();

        void AddToCurrentOperation( ISubOperation operation );

        bool IsChunkActive { get; }

        int OperationCount { get; }

        ObjectTrackingChunkToken GetNewChunkToken();

        ChunkToken StartNamedChunk();

        void EndNamedChunk(ChunkToken token);

        void Clear();
    }
}