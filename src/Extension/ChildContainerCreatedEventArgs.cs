namespace QuickInject
{
    using System;

    public sealed class ChildContainerCreatedEventArgs : EventArgs
    {
        public ChildContainerCreatedEventArgs(ExtensionContext childContext)
        {
            this.ChildContext = childContext;
        }

        public IQuickInjectContainer ChildContainer => this.ChildContext.Container;

        public ExtensionContext ChildContext { get; }
    }
}