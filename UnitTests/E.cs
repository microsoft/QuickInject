// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace UnitTests
{
    internal sealed class E
    {
        public E(IA a1, D d)
        {
            this.A1 = a1;
            this.D = d;
        }

        public IA A1 { get; }

        public D D { get; }
    }
}