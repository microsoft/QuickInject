namespace QuickInject
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Called before every new resolution that QuickInject has not generated a build plan for.  It allows for
    /// inspection or modification of the generated expression tree. 
    /// </summary>
    public interface IBuildPlanVisitor
    {
        /// <summary>
        /// Invoked before the build plan is finalized. 
        /// </summary>
        /// <param name="expression">The expression respresenting the build plan.</param>
        /// <param name="type">The type being resolved.</param>
        /// <returns>The expression to use as the build plan.</returns>
        Expression Visitor(Expression expression, Type type);
    }
}