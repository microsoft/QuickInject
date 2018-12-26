// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace UnitTests
{
    internal sealed class B
    {
        public B(A a1, A a2)
        {
            this.A1 = a1;
            this.A2 = a2;
        }

        public A A1 { get; }

        public A A2 { get; }
    }
}