namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class InvertOperationWrapper : IOperation
    {
        private readonly IOperation wrappedOperation;

        private InvertOperationWrapper(IOperation wrappedOperation)
        {
            this.wrappedOperation = wrappedOperation;
        }

        public void Undo()
        {
            this.wrappedOperation.Redo();
        }
        
        public string Name
        {
            get
            {
                //TODO: Mechanism for generating the reverted operation name
                return this.wrappedOperation.Name;
            }
        }

        public void Redo()
        {
            this.wrappedOperation.Undo();
        }
        
        public static IOperation InvertOperation(IOperation operation)
        {
            InvertOperationWrapper invertOperation;

            return (invertOperation = operation as InvertOperationWrapper) != null ? 
                invertOperation.wrappedOperation : 
                new InvertOperationWrapper( operation );
        }
    }
}