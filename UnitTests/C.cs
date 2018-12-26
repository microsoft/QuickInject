// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public IA GetA2()
        {
            var a = new A { Value = 44 };
            this.PropToVerify = a;
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