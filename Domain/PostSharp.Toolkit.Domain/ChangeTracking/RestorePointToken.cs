namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal sealed class RestorePointToken
    {
        public string Name { get; private set; }

        public RestorePointToken(string name)
        {
            Name = name;
        }
    }
}