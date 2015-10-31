namespace QuickInject
{
    using System;

    public sealed class RegisterEventArgs : EventArgs
    {
        public RegisterEventArgs(Type typeFrom, Type typeTo, LifetimeManager lifetimeManager)
        {
            this.TypeFrom = typeFrom;
            this.TypeTo = typeTo;
            this.LifetimeManager = lifetimeManager;
        }
        
        public Type TypeFrom { get; private set; }

        public Type TypeTo { get; private set; }

        public LifetimeManager LifetimeManager { get; private set; }
    }
}