namespace QuickInject
{
    using System;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.Practices.Unity;

    public abstract class ParameterizedInjectionFactoryBase : InjectionMember
    {
        public Delegate Factory { get; protected set; }

        public override void AddPolicies(Type serviceType, Type implementationType, string name, IPolicyList policies)
        {
            throw new NotSupportedException();
        }
    }
}