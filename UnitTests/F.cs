// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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