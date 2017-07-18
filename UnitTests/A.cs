namespace UnitTests
{
    internal class A : IA
    {
        public A()
        {
            this.Value = 42;
        }

        public int Value { get; set; }
    }
}