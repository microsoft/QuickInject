// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    public interface ICodeProvider
    {
        void GenerateCode(ILGenerator ilgenerator, int lifetimeManagerIndex, Dictionary<Type, int> localsMap);
    }
}