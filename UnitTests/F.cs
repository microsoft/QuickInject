namespace UnitTests
{
    internal sealed class F
    {
        public F(IA a)
        {
            this.Value = a;
        }

        public IA Value { get; }
    }
}