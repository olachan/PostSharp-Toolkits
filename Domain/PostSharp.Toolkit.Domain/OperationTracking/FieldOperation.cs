#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    public class FieldOperation : Operation
    {
        private readonly object oldValue;

        private readonly object newValue;

        private readonly FieldInfoWithCompiledAccessors fieldAccessor;

        public FieldOperation( ITrackable target, Type implementingType, string fieldFullName, object oldValue, object newValue )
            : base(target)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.fieldAccessor = ObjectAccessorsMap.Map[implementingType].FieldAccessors[fieldFullName];
        }

        public override void Undo()
        {
            this.CheckValues(newValue);

            fieldAccessor.SetValue(this.Target, oldValue);
        }

        public override void Redo()
        {
            this.CheckValues( oldValue );

            fieldAccessor.SetValue(this.Target, newValue);
        }

        [Conditional("DEBUG")]
        private void CheckValues(object expectedValue)
        {
            if (!Equals(fieldAccessor.GetValue(this.Target), expectedValue))
            {
                throw new ArgumentException("Wrong value");
            }
        }
    }
}