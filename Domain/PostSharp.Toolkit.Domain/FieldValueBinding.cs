using System;
using System.Reflection;

namespace PostSharp.Toolkit.Domain
{
    [Serializable]
    internal sealed class FieldValueBinding : IEquatable<FieldValueBinding>
    {
        public FieldValueBinding(string propertyName, FieldInfoWithCompiledGetter field, bool isActive)
        {
            this.PropertyName = propertyName;
            this.Field = field;
            this.IsActive = isActive;
        }

        public FieldValueBinding(FieldValueBinding prototype)
        {
            this.Field = prototype.Field;
            this.PropertyName = prototype.PropertyName;
            this.IsActive = prototype.IsActive;
        }

        public FieldInfoWithCompiledGetter Field { get; private set; }

        public bool IsActive { get; set; }

        public string PropertyName { get; private set; }

        public override bool Equals(object obj)
        {
            FieldValueBinding other = obj as FieldValueBinding;
            return this.Equals(other);
        }

        public bool Equals(FieldValueBinding other)
        {
            if (other == null)
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

    // TODO: should be compiled and stored per field not per binding
    // Binding to field with compiled getter for performance
}