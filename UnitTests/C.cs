namespace UnitTests
{
    using System;

    internal sealed class C
    {
        public C(B b, A a)
        {
        }

        public IA PropToVerify { get; private set; }

        public IA GetA()
        {
            this.PropToVerify = new A();
            return this.PropToVerify;
        }

        public IA ThrowException()
        {
            throw new Exception("This should not hang the unit test");
        }
    }
}