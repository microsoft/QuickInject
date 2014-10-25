namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.Practices.Unity;

    internal sealed class DummyBuilderContext : IBuilderContext
    {
        public IStrategyChain Strategies { get; private set; }

        public ILifetimeContainer Lifetime { get; private set; }

        public NamedTypeBuildKey OriginalBuildKey { get; private set; }

        public NamedTypeBuildKey BuildKey { get; set; }

        public IPolicyList PersistentPolicies { get; private set; }

        public IPolicyList Policies { get; private set; }

        public IRecoveryStack RecoveryStack { get; private set; }

        public object Existing { get; set; }

        public bool BuildComplete { get; set; }

        public object CurrentOperation { get; set; }

        public IBuilderContext ChildContext { get; private set; }

        public void AddResolverOverrides(IEnumerable<ResolverOverride> newOverrides)
        {
            throw new NotImplementedException();
        }

        public IDependencyResolverPolicy GetOverriddenResolver(Type dependencyType)
        {
            throw new NotImplementedException();
        }

        public object NewBuildUp(NamedTypeBuildKey newBuildKey)
        {
            throw new NotImplementedException();
        }

        public object NewBuildUp(NamedTypeBuildKey newBuildKey, Action<IBuilderContext> childCustomizationBlock)
        {
            throw new NotImplementedException();
        }
    }
}