// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection.Emit;

    internal sealed class ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression : Expression
    {
        private readonly InjectionMember injectionMember;

        private readonly int lifetimeManagerIndex;

        public ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(InjectionMember injectionMember, int lifetimeManagerIndex)
        {
            this.injectionMember = injectionMember;
            this.lifetimeManagerIndex = lifetimeManagerIndex;
        }

        public IEnumerable<Type> DependentTypes => this.injectionMember.DependentTypes;

        public void GenerateCode(ILGenerator ilGenerator, Dictionary<Type, int> localsMap)
        {
            this.injectionMember.CodeProvider?.GenerateCode(ilGenerator, this.lifetimeManagerIndex, localsMap);
        }
    }
}