namespace PostSharp.Toolkit.Domain
{
    internal interface INestableContext
    {
        void Push( NestableContextInfo context );

        NestableContextInfo Pop();

        NestableContextInfo Current { get; }
    }
}