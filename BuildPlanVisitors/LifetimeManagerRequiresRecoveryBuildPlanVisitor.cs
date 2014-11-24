namespace QuickInject.BuildPlanVisitors
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.Practices.ObjectBuilder2;

    public sealed class LifetimeManagerRequiresRecoveryBuildPlanVisitor : IBuildPlanVisitor
    {
        public Expression Visitor(Expression expression, Type type)
        {
            var visitor = new LifetimeManagerRequiresRecoveryExpressionVisitor();
            Expression modifiedExpression = visitor.Visit(expression);
            return modifiedExpression;
        }

        private sealed class LifetimeManagerRequiresRecoveryExpressionVisitor : ExpressionVisitor
        {
            private static readonly Type RequiresRecoveryType = typeof(IRequiresRecovery);

            private static readonly MethodInfo RecoverMethodInfo = RequiresRecoveryType.GetRuntimeMethod("Recover", new Type[] { });

            protected override Expression VisitConditional(ConditionalExpression node)
            {
                var equalsOperation = node.Test as BinaryExpression;
                if (equalsOperation != null)
                {
                    var assign = equalsOperation.Left as BinaryExpression;
                    if (assign != null)
                    {
                        var result = assign.Left as ParameterExpression;
                        var typeAs = assign.Right as UnaryExpression;
                        if (result != null && typeAs != null)
                        {
                            var methodCall = typeAs.Operand as MethodCallExpression;
                            if (methodCall != null && methodCall.Object != null && methodCall.Object.Type.GetTypeInfo().ImplementedInterfaces.Any(t => t == RequiresRecoveryType))
                            {
                                return base.VisitConditional(Expression.Condition(node.Test, Expression.TryCatch(node.IfTrue, Expression.Catch(typeof(Exception), Expression.Block(Expression.Call(methodCall.Object, RecoverMethodInfo), Expression.Rethrow(), result))), node.IfFalse));
                            }
                        }
                    }
                }

                return base.VisitConditional(node);
            }
        }
    }
}