namespace QuickInject
{
    using System;

    public sealed class ChildContainerCreatedEventArgs : EventArgs
    {
        public ChildContainerCreatedEventArgs(ExtensionContext childContext)
        {
            this.ChildContext = childContext;
        }

        public IQuickInjectContainer ChildContainer
        {
            get { return this.ChildContext.Container; }
        }

        public ExtensionContext ChildContext { get; private set; }
    }
}