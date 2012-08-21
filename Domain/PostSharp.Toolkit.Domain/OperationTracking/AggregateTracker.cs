//#region Copyright (c) 2012 by SharpCrafters s.r.o.
//// Copyright (c) 2012, SharpCrafters s.r.o.
//// All rights reserved.
//// 
//// For licensing terms, see file License.txt
//#endregion

//using System.Collections.Generic;

//namespace PostSharp.Toolkit.Domain.OperationTracking
//{
//    public class AggregateTracker : ObjectTracker
//    {
//        private readonly List<WeakReference<IObjectTracker>> dependentTrackers;

//        public AggregateTracker(ITrackable target)
//            : base(target)
//        {
//            this.dependentTrackers = new List<WeakReference<IObjectTracker>>();
//        }

//        public AggregateTracker AddDependentTracker(IObjectTracker tracker)
//        {
//            this.dependentTrackers.Add(new WeakReference<IObjectTracker>(tracker));
//            tracker.SetParentTracker( this );
//            return this;
//        }

//        public AggregateTracker RemoveDependentTracker(IObjectTracker tracker)
//        {
//            var trackerRef = this.dependentTrackers.Find( t => ReferenceEquals( t.Target, tracker ) );

//            if (trackerRef == null) // TODO why null?
//            {
//                return this;
//            }

//            trackerRef.Target.SetParentTracker( this.ParentTracker );

//            this.dependentTrackers.Remove(trackerRef);
            
//            return this;
//        }

//        //public override void AddObjectSnapshot( string name = null )
//        //{
//        //    if (dependentTrackers.Count == 0)
//        //    {
//        //        base.AddObjectSnapshot( name );
//        //    }

//        //    List<IOperation> snapshots = this.GetAllSnapshots();
//        //    if (snapshots == null)
//        //    {
//        //        return;
//        //    }

//        //    BatchOperation operation = new BatchOperation( new Stack<IOperation>(snapshots) );

//        //    if (name != null)
//        //    {
//        //        operation.ConvertToNamedRestorePoint(name);
//        //    }

//        //    this.AddOperation(operation);
//        //}

//        //private List<IOperation> GetAllSnapshots()
//        //{
//        //    IOperation targetSnapshot = this.GetTargetSnapshot();

//        //    if (targetSnapshot == null)
//        //    {
//        //        return null;
//        //    }

//        //    List<IOperation> snapshots = new List<IOperation> { targetSnapshot };

//        //    foreach (WeakReference<IObjectTracker> dependentTracker in this.dependentTrackers)
//        //    {
//        //        ITrackable trackable = dependentTracker.Target;
//        //        if ( trackable == null )
//        //        {
//        //            continue;
//        //        }

//        //        IOperation trackerSnapshot = trackable.TakeSnapshot();
//        //        if ( trackerSnapshot != null )
//        //        {
//        //            snapshots.Add( trackerSnapshot );
//        //        }
//        //    }

//        //    return snapshots;
//        //}

//        protected override IOperation TakeSnapshot()
//        {
//            ITrackable trackable = this.Target;

//            if (trackable != null)
//            {
//                List<IOperation> currentSnapshots = this.GetAllSnapshots();

//                return new ObjectTrackerOperation(
//                    this,
//                    this.undoOperations.Clone(),
//                    this.redoOperations.Clone(),
//                    currentSnapshots);
//            }

//            return null;
//        }
//    }
//}