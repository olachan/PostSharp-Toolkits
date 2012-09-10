#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Diagnostics;

using PostSharp.Toolkit.Domain.Common;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public class FieldValueChange : SubOperation
    {
        private readonly object target;

        private readonly object oldValue;

        private readonly object newValue;

        private readonly FieldInfoWithCompiledAccessors fieldAccessor;

        public FieldValueChange(object target, Type implementingType, string fieldFullName, object oldValue, object newValue)
        {
            this.target = target;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.fieldAccessor = ObjectAccessorsMap.Map[implementingType].FieldAccessors[fieldFullName];
        }

        protected internal override void Undo()
        {
            this.CheckValues(this.newValue);

            this.fieldAccessor.SetValue(this.target, this.oldValue);
        }

        protected internal override void Redo()
        {
            this.CheckValues( this.oldValue );

            this.fieldAccessor.SetValue(this.target, this.newValue);
        }

        [Conditional("DEBUG")]
        private void CheckValues(object expectedValue)
        {
            if (!Equals(this.fieldAccessor.GetValue(this.target), expectedValue))
            {
                throw new ArgumentException("Wrong value");
            }
        }
    }
}