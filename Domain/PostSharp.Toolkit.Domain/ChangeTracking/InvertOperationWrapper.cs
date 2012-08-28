namespace PostSharp.Toolkit.Domain.ChangeTracking
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
        
        public string Name
        {
            get
            {
                //TODO: Mechanism for generating the reverted operation name
                return this.wrapedOperation.Name;
            }
        }

        public void Redo()
        {
            this.wrapedOperation.Undo();
        }

        //TODO: Factory method to avoid creating multiple layers of wrappers (constructor should be private)
    }
}