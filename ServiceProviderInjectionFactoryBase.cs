namespace QuickInject
{
    using System;
    using System.Linq.Expressions;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.Practices.Unity;

    public abstract class ServiceProviderInjectionFactoryBase : InjectionMember
    {
        public MethodCallExpression Factory { get; protected set; }

        public override void AddPolicies(Type serviceType, Type implementationType, string name, IPolicyList policies)
        {
            throw new NotSupportedException();
        }
    }
}