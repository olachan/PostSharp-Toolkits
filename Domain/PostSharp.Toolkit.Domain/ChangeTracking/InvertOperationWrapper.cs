namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal class InvertOperationWrapper : Operation
    {
        private readonly Operation wrappedOperation;

        private InvertOperationWrapper(Operation wrappedOperation)
        {
            this.wrappedOperation = wrappedOperation;
            this.Name = string.Format( "Undo - {0}", wrappedOperation.Name );
        }

        private InvertOperationWrapper(Operation wrappedOperation, string nameFormat)
        {
            this.wrappedOperation = wrappedOperation;
            this.Name = string.Format( nameFormat, wrappedOperation.Name );
        }

        protected internal override void Undo()
        {
            this.wrappedOperation.Redo();
        }

        protected internal override void Redo()
        {
            this.wrappedOperation.Undo();
        }
        
        public static Operation InvertOperation(Operation operation)
        {
            InvertOperationWrapper invertOperation;

            return (invertOperation = operation as InvertOperationWrapper) != null ? 
                invertOperation.wrappedOperation : 
                new InvertOperationWrapper( operation );
        }

        public static Operation InvertOperation(Operation operation, string nameFormat)
        {
            InvertOperationWrapper invertOperation;

            return (invertOperation = operation as InvertOperationWrapper) != null ?
                invertOperation.wrappedOperation :
                new InvertOperationWrapper(operation, nameFormat);
        }
    }
}