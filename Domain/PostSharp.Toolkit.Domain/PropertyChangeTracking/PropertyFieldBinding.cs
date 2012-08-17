#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.PropertyChangeTracking
{
    /// <summary>
    /// Represents heuristically identified association of property value with a field.
    /// Denotes that the last time the property getter was called it returned value of the field.
    /// </summary>
    [Serializable]
    internal sealed class PropertyFieldBinding : IEquatable<PropertyFieldBinding>
    {
        public PropertyFieldBinding( string propertyName, FieldInfoWithCompiledGetter field, bool isActive )
        {
            this.PropertyName = propertyName;
            this.Field = field;
            this.IsActive = isActive;
        }

        public PropertyFieldBinding( PropertyFieldBinding prototype )
        {
            this.Field = prototype.Field;
            this.PropertyName = prototype.PropertyName;
            this.IsActive = prototype.IsActive;
        }

        public FieldInfoWithCompiledGetter Field { get; private set; }

        public bool IsActive { get; set; }

        public string PropertyName { get; private set; }

        public override bool Equals( object obj )
        {
            PropertyFieldBinding other = obj as PropertyFieldBinding;
            return this.Equals( other );
        }

        public bool Equals( PropertyFieldBinding other )
        {
            if ( other == null )
            {
                return false;
            }

            return this.Field.FieldName == other.Field.FieldName && this.IsActive == other.IsActive;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Field != null ? this.Field.FieldName.GetHashCode() : 0) * 397) ^ this.IsActive.GetHashCode();
            }
        }
    }
}