namespace QuickInject
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.Practices.Unity;

    public sealed class InjectionFactory : ServiceProviderInjectionFactoryBase
    {
        private static readonly IUnityContainer DummyContainer = new UnityContainer();

        public InjectionFactory(Func<IUnityContainer, object> factoryFunc)
        {
            var parameters = new Expression[] { Expression.Constant(DummyContainer) };
            this.Factory = factoryFunc.Target == null ? Expression.Call(factoryFunc.GetMethodInfo(), parameters) : Expression.Call(Expression.Constant(factoryFunc.Target), factoryFunc.GetMethodInfo(), parameters);
        }
    }
}