// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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