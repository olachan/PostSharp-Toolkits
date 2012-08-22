namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public interface IObjectTracker : ITracker, ITrackable
    {
        ITracker ParentTracker { get; set; } //TODO: internal interface?

        void StartChunk();

        void EndChunk();

        void AddOperationToChunk(IOperation operation);

        bool IsChunkActive { get; }

        int OperationCount { get; }

        ObjectTrackingChunkToken GetNewChunkToken();

        ChunkToken StartNamedChunk();

        void EndNamedChunk(ChunkToken token);
    }
}