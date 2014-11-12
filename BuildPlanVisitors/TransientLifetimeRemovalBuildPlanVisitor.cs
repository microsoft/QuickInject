namespace QuickInject.BuildPlanVisitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.Practices.Unity;

    public sealed class TransientLifetimeRemovalBuildPlanVisitor : IBuildPlanVisitor
    {
        public Expression Visitor(Expression expression, Type type, bool slowPath)
        {
            var deduper = new TransientLifetimeRemovalExpressionVisitor();
            var emptyExpressionVisitor = new EmptyExpressionVisitor();
            var modifiedExpression = emptyExpressionVisitor.Visit(deduper.Visit(expression));
            return modifiedExpression;
        }

        private sealed class EmptyExpressionVisitor : ExpressionVisitor
        {
            protected override Expression VisitBlock(BlockExpression node)
            {
                var modifiedExpressionList = new List<Expression>();
                foreach (var expression in node.Expressions)
                {
                    if (expression.NodeType == ExpressionType.Default && expression.Type == typeof(void))
                    {
                        continue;
                    }
                    else
                    {
                        modifiedExpressionList.Add(expression);
                    }
                }

                return base.VisitBlock(Expression.Block(node.Variables, modifiedExpressionList));
            }
        }

        private sealed class TransientLifetimeRemovalExpressionVisitor : ExpressionVisitor
        {
            private static Type TransientLifetimeManagerType = typeof(TransientLifetimeManager);

            private static MethodInfo SetValueMethodInfo = TransientLifetimeManagerType.GetRuntimeMethod("SetValue", new[] { typeof(object) });

            protected override Expression VisitBlock(BlockExpression node)
            {
                var modifiedExpressionList = new List<Expression>();
                foreach (var expression in node.Expressions)
                {
                    if (expression.NodeType == ExpressionType.Call)
                    {
                        var methodCall = (MethodCallExpression)expression;
                        if (methodCall != null && methodCall.Object != null && methodCall.Object.Type == TransientLifetimeManagerType && methodCall.Method == SetValueMethodInfo)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        modifiedExpressionList.Add(expression);
                    }
                }

                return base.VisitBlock(Expression.Block(node.Variables, modifiedExpressionList));
            }

            protected override Expression VisitConditional(ConditionalExpression node)
            {
                var equalsOperation = node.Test as BinaryExpression;
                if (equalsOperation != null)
                {
                    var assign = equalsOperation.Left as BinaryExpression;
                    if (assign != null)
                    {
                        var typeAs = assign.Right as UnaryExpression;
                        if (typeAs != null)
                        {
                            var methodCall = typeAs.Operand as MethodCallExpression;
                            if (methodCall != null && methodCall.Object != null && methodCall.Object.Type == TransientLifetimeManagerType)
                            {
                                return base.Visit(node.IfTrue);
                            }
                        }
                    }
                }

                return base.VisitConditional(node);
            }
        }
    }
}