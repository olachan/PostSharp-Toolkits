using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    //TODO: Need to seriously limit surface area of the toolkit. This interface exposes way too much...
    //TODO: We should stick to static class as the public API for object tracking. Consider removing this interface totally or making it a marker only
    public interface IObjectTracker : ITracker
    {
        ITracker ParentTracker { get; }

        //void StartChunk();

        //void EndChunk();

        void AddToCurrentOperation( ISubOperation operation );

        bool IsOperationOpen { get; }

        int OperationsCount { get; }

        IDisposable StartAtomicOperation();

        //ChunkToken StartNamedChunk();

        //void EndNamedChunk(ChunkToken token);

        void Clear();

        void AssociateWithParent( ITracker globalTracker );
        void StartImplicitOperation();
        void EndImplicitOperation();
    }
}