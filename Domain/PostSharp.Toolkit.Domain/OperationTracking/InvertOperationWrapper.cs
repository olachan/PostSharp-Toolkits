namespace PostSharp.Toolkit.Domain.OperationTracking
{
    internal class InvertOperationWrapper : IOperation
    {
        private readonly IOperation wrapedOperation;

        public InvertOperationWrapper(IOperation wrapedOperation)
        {
            this.wrapedOperation = wrapedOperation;
        }

        public void Undo()
        {
            this.wrapedOperation.Redo();
        }

        public bool IsNamedRestorePoint
        {
            get
            {
                return this.wrapedOperation.IsNamedRestorePoint;
            }
        }

        public string Name
        {
            get
            {
                return this.wrapedOperation.Name;
            }
        }

        public void ConvertToNamedRestorePoint(string name)
        {
            this.wrapedOperation.ConvertToNamedRestorePoint(name);
        }

        public void Redo()
        {
            this.wrapedOperation.Undo();
        }
    }
}