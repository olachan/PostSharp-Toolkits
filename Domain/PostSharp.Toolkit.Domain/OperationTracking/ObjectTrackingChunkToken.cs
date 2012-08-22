using System;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public class ObjectTrackingChunkToken : IDisposable
    {
        private readonly IObjectTracker objectTracker;

        private ChunkToken token;

        internal ObjectTrackingChunkToken(IObjectTracker objectTracker)
        {
            this.objectTracker = objectTracker;
            this.token = this.objectTracker.StartNamedChunk();
        }

        public void Dispose()
        {
            this.objectTracker.EndNamedChunk( token );
        }
    }
}