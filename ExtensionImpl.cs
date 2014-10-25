namespace QuickInject
{
    using System;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.Practices.Unity;
    using Microsoft.Practices.Unity.ObjectBuilder;

    internal sealed class ExtensionImpl : ExtensionContext
    {
        private readonly QuickInjectContainer container;

        private readonly DummyPolicyList policyList;

        public ExtensionImpl(QuickInjectContainer container, DummyPolicyList policyList)
        {
            this.container = container;
            this.policyList = policyList;
        }

        public override event EventHandler<RegisterEventArgs> Registering
        {
            add
            {
                this.container.Registering += value;
            }

            remove
            {
                this.container.Registering -= value;
            }
        }

        public override event EventHandler<RegisterInstanceEventArgs> RegisteringInstance
        {
            add
            {
                this.container.RegisteringInstance += value;
            }

            remove
            {
                this.container.RegisteringInstance -= value;
            }
        }

        public override event EventHandler<ChildContainerCreatedEventArgs> ChildContainerCreated
        {
            add
            {
                this.container.ChildContainerCreated += value;
            }

            remove
            {
                this.container.ChildContainerCreated -= value;
            }
        }

        public override IUnityContainer Container
        {
            get
            {
                return this.container;
            }
        }

        public override StagedStrategyChain<UnityBuildStage> Strategies
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override StagedStrategyChain<UnityBuildStage> BuildPlanStrategies
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override IPolicyList Policies
        {
            get
            {
                return this.policyList;
            }
        }

        public override ILifetimeContainer Lifetime
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override void RegisterNamedType(Type t, string name)
        {
            throw new NotSupportedException();
        }
    }
}