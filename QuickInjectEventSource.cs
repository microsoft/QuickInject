namespace QuickInject
{
    using System;
    using System.Diagnostics.Tracing;
    using Microsoft.Practices.Unity;

    internal sealed class QuickInjectEventSource : EventSource
    {
        [NonEvent]
        public void RegisterType(Type from, Type to, LifetimeManager lifetime)
        {
            this.RegisterType(from.ToString(), to.ToString(), lifetime == null ? string.Empty : lifetime.GetType().ToString());
        }

        public void RegisterType(string from, string to, string lifetimeType)
        {
            this.WriteEvent(1, from, to, lifetimeType);
        }

        public void FastResolve(string type)
        {
            this.WriteEvent(2, type);
        }

        public void SlowResolve(string type)
        {
            this.WriteEvent(3, type);
        }

        public void UnregisteredResolve(string type)
        {
            this.WriteEvent(4, type);
        }
    }
}