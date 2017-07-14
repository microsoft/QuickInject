namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection.Emit;

    internal sealed class ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression : Expression
    {
        private readonly InjectionMember injectionMember;

        public ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(InjectionMember injectionMember)
        {
            this.injectionMember = injectionMember;
        }

        public IEnumerable<Type> DependentTypes => this.injectionMember.DependentTypes;

        public void GenerateCode(ILGenerator ilGenerator, Dictionary<Type, int> localsMap)
        {
            this.injectionMember.CodeProvider?.GenerateCode(ilGenerator, localsMap);
        }
    }
}