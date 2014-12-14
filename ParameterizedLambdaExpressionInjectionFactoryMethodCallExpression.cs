namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    internal sealed class ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression : Expression
    {
        private readonly LambdaExpression expression;

        public ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(LambdaExpression lambdaExpression)
        {
            this.expression = lambdaExpression;
        }

        public IEnumerable<Type> DependentTypes
        {
            get
            {
                return this.expression.Parameters.Select(t => t.Type);
            }
        }

        public Expression Resolve(Type resolveType, ParameterExpression output, Stack<Expression> dependentExpressionStack, Dictionary<Type, Stack<ParameterExpression>> dataProvider)
        {
            var expressions = new List<Expression>();
            for (int i = 0; i < this.DependentTypes.Count(); ++i)
            {
                expressions.Add(dependentExpressionStack.Pop());
            }

            expressions.Add(Expression.Assign(output, Expression.TypeAs(new ParameterVisitor(dataProvider).Visit(this.expression.Body), resolveType)));

            return Expression.Block(expressions);
        }

        private sealed class ParameterVisitor : ExpressionVisitor
        {
            private readonly Dictionary<Type, Stack<ParameterExpression>> variableReplacementProvider;

            public ParameterVisitor(Dictionary<Type, Stack<ParameterExpression>> dataProvider)
            {
                this.variableReplacementProvider = dataProvider;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return this.variableReplacementProvider[node.Type].Peek();
            }
        }
    }
}