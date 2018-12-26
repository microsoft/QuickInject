// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using Microsoft.QuickInject;

    internal sealed class GetA2CodeProvider : ICodeProvider
    {
        public void GenerateCode(ILGenerator ilGenerator, int lifetimeManagerIndex, Dictionary<Type, int> localsMap)
        {
            ilGenerator.Emit(OpCodes.Ldloc, localsMap[typeof(C)]);
            ilGenerator.Emit(OpCodes.Call, typeof(C).GetMethod("GetA2"));
        }
    }
}