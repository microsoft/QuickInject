namespace QuickInject.BuildPlanVisitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    internal sealed class EmptyBlockExpressionRemovalBuildPlanVisitor : IBuildPlanVisitor
    {
        public Expression Visitor(Expression expression, Type type)
        {
            return new EmptyBlockExpressionRemovalVisitor().Visit(expression);
        }

        private sealed class EmptyBlockExpressionRemovalVisitor : ExpressionVisitor
        {
            protected override Expression VisitBlock(BlockExpression blockExpression)
            {
                if (blockExpression.Variables.Count == 0)
                {
                    var list = new List<Expression>();
                    this.VisitBlockInternal(list, blockExpression);
                    return base.VisitBlock(Expression.Block(list));
                }
                else
                {
                    return base.VisitBlock(blockExpression);
                }
            }

            private void VisitBlockInternal(List<Expression> expressions, BlockExpression blockExpression)
            {
                foreach (var expression in blockExpression.Expressions)
                {
                    if (expression.NodeType == ExpressionType.Block)
                    {
                        this.VisitBlockInternal(expressions, (BlockExpression)expression);
                    }
                    else
                    {
                        expressions.Add(expression);
                    }
                }
            }
        }
    }
}