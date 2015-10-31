namespace QuickInject
{
    using System;
    using System.Linq.Expressions;

    internal sealed class TypeRegistration
    {
        public TypeRegistration(Type registrationType, Type mappedToType, LifetimeManager lifetimeManager, Expression factory)
        {
            this.RegistrationType = registrationType;
            this.MappedToType = mappedToType;
            this.LifetimeManager = lifetimeManager;
            this.Factory = factory;
        }

        public Type RegistrationType { get; private set; }

        public Type MappedToType { get; set; }

        public LifetimeManager LifetimeManager { get; set; }

        public Expression Factory { get; set; }
    }
}