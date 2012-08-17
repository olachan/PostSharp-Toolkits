#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public  class FieldSnapshot : Snapshot
    {
        public Dictionary<int, object> FieldValues { get; private set; }

        public FieldSnapshot( IOperationTrackable target, Dictionary<int, object> fieldValues )
            : base(target)
        {
            this.FieldValues = fieldValues;
        }
    }
}