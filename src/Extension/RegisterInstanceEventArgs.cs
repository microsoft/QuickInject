namespace QuickInject
{
    using System;

    public sealed class RegisterInstanceEventArgs : EventArgs
    {
        public RegisterInstanceEventArgs(Type registeredType, object instance, LifetimeManager lifetimeManager)
        {
            this.RegisteredType = registeredType;
            this.Instance = instance;
            this.LifetimeManager = lifetimeManager;
        }
        
        public Type RegisteredType { get; private set; }

        public object Instance { get; private set; }

        public LifetimeManager LifetimeManager { get; private set; }
    }
}