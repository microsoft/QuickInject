// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace UnitTests
{
    internal sealed class D
    {
        public D(IA a1, IA a2)
        {
            this.A1 = a1;
            this.A2 = a2;
        }

        public IA A1 { get; }

        public IA A2 { get; }
    }
}