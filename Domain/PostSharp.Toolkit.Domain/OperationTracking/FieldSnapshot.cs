#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.Collections.Generic;
using System.Linq;

using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public  class FieldSnapshot : Snapshot
    {
        public Dictionary<int, object> FieldValues { get; private set; }

        public FieldSnapshot( IOperationTrackable target )
            : base(target)
        {
            Dictionary<int, FieldInfoWithCompiledAccessors> fieldAccessors = ObjectAccessorsMap.Map[target.GetType()].FieldAccessors;
            Dictionary<int, object> fieldValues = fieldAccessors.ToDictionary(f => f.Key, f => f.Value.GetValue(target));
            this.FieldValues = fieldValues;
        }

        public override void Restore()
        {
            object instance = this.Target.Target;

            if (instance == null)
            {
                return;
            }

            Dictionary<int, FieldInfoWithCompiledAccessors> fieldAccessors = ObjectAccessorsMap.Map[instance.GetType()].FieldAccessors;

            foreach (KeyValuePair<int, FieldInfoWithCompiledAccessors> fieldAccessor in fieldAccessors)
            {
                fieldAccessor.Value.SetValue(instance, this.FieldValues[fieldAccessor.Key]);
            }
        }
    }
}