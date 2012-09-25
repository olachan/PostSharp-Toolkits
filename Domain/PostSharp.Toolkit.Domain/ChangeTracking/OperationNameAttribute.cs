using System;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    public sealed class OperationNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public OperationNameAttribute(string name)
        {
            this.Name = name;
        }
    }
}