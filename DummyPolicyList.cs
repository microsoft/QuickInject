namespace QuickInject
{
    using System;
    using Microsoft.Practices.ObjectBuilder2;

    internal sealed class DummyPolicyList : IPolicyList
    {
        public IPropertySelectorPolicy PropertySelectorPolicy { get; set; }

        public void Clear(Type policyInterface, object buildKey)
        {
            throw new NotImplementedException();
        }

        public void ClearAll()
        {
            throw new NotImplementedException();
        }

        public void ClearDefault(Type policyInterface)
        {
            throw new NotImplementedException();
        }

        public IBuilderPolicy Get(Type policyInterface, object buildKey, bool localOnly, out IPolicyList containingPolicyList)
        {
            throw new NotImplementedException();
        }

        public IBuilderPolicy GetNoDefault(Type policyInterface, object buildKey, bool localOnly, out IPolicyList containingPolicyList)
        {
            throw new NotImplementedException();
        }

        public void Set(Type policyInterface, IBuilderPolicy policy, object buildKey)
        {
            throw new NotImplementedException();
        }

        public void SetDefault(Type policyInterface, IBuilderPolicy policy)
        {
            if (policyInterface == typeof(IPropertySelectorPolicy))
            {
                this.PropertySelectorPolicy = (IPropertySelectorPolicy)policy;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}