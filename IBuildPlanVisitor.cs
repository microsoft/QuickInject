namespace QuickInject
{
    using System;
    using System.Linq.Expressions;

    public interface IBuildPlanVisitor
    {
        Expression Visitor(Expression expression, Type type, bool slowPath);
    }
}