namespace QuickInject
{
    using System;

    public abstract class ExtensionContext
    {
        public abstract IQuickInjectContainer Container { get; }
        
        public abstract event EventHandler<RegisterEventArgs> Registering;
        
        public abstract event EventHandler<RegisterInstanceEventArgs> RegisteringInstance;

        public abstract event EventHandler<ChildContainerCreatedEventArgs> ChildContainerCreated;
    }
}