namespace QuickInject
{
    using System.Linq.Expressions;
    using Microsoft.Practices.Unity;

    internal sealed class InjectionFactoryMethodCallExpression : Expression
    {
        private readonly MethodCallExpression expression;

        public InjectionFactoryMethodCallExpression(MethodCallExpression expression)
        {
            this.expression = expression;
        }

        public Expression Resolve(IUnityContainer container)
        {
            var oldArguments = this.expression.Arguments;
            var newArguments = new Expression[oldArguments.Count];

            newArguments[0] = Expression.Constant(container);
            for (int i = 1; i < oldArguments.Count; ++i)
            {
                newArguments[i] = oldArguments[i];
            }

            return this.expression.Object == null ? Expression.Call(this.expression.Method, newArguments) : Expression.Call(this.expression.Object, this.expression.Method, newArguments);
        }
    }
}