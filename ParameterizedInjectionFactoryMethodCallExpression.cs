namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    internal sealed class ParameterizedInjectionFactoryMethodCallExpression : Expression
    {
        private readonly object instance;

        private readonly MethodInfo methodInfo;

        private readonly List<Expression> parameterExpressions;

        public ParameterizedInjectionFactoryMethodCallExpression(object instance, MethodInfo methodInfo)
        {
            this.instance = instance;
            this.methodInfo = methodInfo;
            this.parameterExpressions = new List<Expression>();
        }

        public IEnumerable<Type> DependentTypes
        {
            get
            {
                return this.methodInfo.GetParameters().Select(t => t.ParameterType);
            }
        }

        public Expression Resolve(Dictionary<Type, Stack<ParameterExpression>> dataProvider)
        {
            var parameters = this.methodInfo.GetParameters();
            foreach (var parameter in parameters)
            {
                this.parameterExpressions.Add(dataProvider[parameter.ParameterType].Peek());
            }

            return this.instance != null
                       ? Expression.Call(Expression.Constant(this.instance), this.methodInfo, this.parameterExpressions)
                       : Expression.Call(this.methodInfo, this.parameterExpressions);
        }
    }
}