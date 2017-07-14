namespace QuickInject
{
    using System;

    public sealed class RegisterEventArgs : EventArgs
    {
        public RegisterEventArgs(Type typeFrom, Type typeTo, LifetimeManager lifetimeManager)
        {
            this.TypeFrom = typeFrom;
            this.TypeTo = typeTo ?? throw new ArgumentNullException(nameof(typeTo));
            this.LifetimeManager = lifetimeManager;
        }

        public Type TypeFrom { get; }

        public Type TypeTo { get; }

        public LifetimeManager LifetimeManager { get; }
    }
}