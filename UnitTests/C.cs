namespace UnitTests
{
    using System;

    internal sealed class C
    {
        public C(B b, A a)
        {
        }

        public IA PropToVerify { get; private set; }

        public D PropToVerify2 { get; private set; }

        public IA GetA()
        {
            this.PropToVerify = new A();
            return this.PropToVerify;
        }

        public D GetD()
        {
            this.PropToVerify2 = new D(new A(), new A());
            return this.PropToVerify2;
        }

        public IA ThrowException()
        {
            throw new Exception("This should not hang the unit test");
        }
    }
}