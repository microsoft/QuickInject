namespace QuickInject
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public sealed class ServiceProviderInjectionFactory<T> : ServiceProviderInjectionFactoryBase
    {
        public ServiceProviderInjectionFactory(Func<IServiceProvider, T> func, IServiceProvider serviceProvider)
        {
            var parameters = new Expression[] { Expression.Constant(serviceProvider) };
            this.Factory = func.Target == null ? Expression.Call(func.GetMethodInfo(), parameters) : Expression.Call(Expression.Constant(func.Target), func.GetMethodInfo(), parameters);
        }
    }
}