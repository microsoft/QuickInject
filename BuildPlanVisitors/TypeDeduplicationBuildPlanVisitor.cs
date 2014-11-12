namespace QuickInject.BuildPlanVisitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public sealed class TypeDeduplicationBuildPlanVisitor : IBuildPlanVisitor
    {
        public Expression Visitor(Expression expression, Type type, bool slowPath)
        {
            var deduper = new TypeDeduplicationExpressionVisitor();
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

        private sealed class TypeDeduplicationExpressionVisitor : ExpressionVisitor
        {
            private Dictionary<Type, ParameterExpression> typeToParameterExpressionTable = new Dictionary<Type, ParameterExpression>();

            private HashSet<ParameterExpression> parameters = new HashSet<ParameterExpression>();

            private bool topMostBlockExpressionVisited;

            protected override Expression VisitBlock(BlockExpression node)
            {
                if (!this.topMostBlockExpressionVisited)
                {
                    foreach (var variable in node.Variables)
                    {
                        if (!this.typeToParameterExpressionTable.ContainsKey(variable.Type))
                        {
                            this.typeToParameterExpressionTable.Add(variable.Type, variable);
                            this.parameters.Add(variable);
                        }
                    }

                    this.topMostBlockExpressionVisited = true;
                    return base.VisitBlock(Expression.Block(this.typeToParameterExpressionTable.Values, node.Expressions));
                }

                return base.VisitBlock(node);
            }

            protected override Expression VisitConditional(ConditionalExpression node)
            {
                var equalsOperation = node.Test as BinaryExpression;
                if (equalsOperation != null)
                {
                    var assign = equalsOperation.Left as BinaryExpression;
                    if (assign != null)
                    {
                        var parameter = assign.Left as ParameterExpression;
                        if (parameter != null && !this.parameters.Contains(parameter))
                        {
                            return Expression.Empty();
                        }
                    }
                }

                return base.VisitConditional(node);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (this.typeToParameterExpressionTable.ContainsKey(node.Type))
                {
                    return base.VisitParameter(this.typeToParameterExpressionTable[node.Type]);
                }

                return base.VisitParameter(node);
            }
        }
    }
}