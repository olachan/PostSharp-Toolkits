namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal struct NamedRestorePoint
    {
        public string Name { get; private set; }

        public int Index { get; private set; }

        public NamedRestorePoint( string name, int index )
            : this()
        {
            this.Name = name;
            this.Index = index;
        }
    }
}