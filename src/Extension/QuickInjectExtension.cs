namespace QuickInject
{
    using System;

    public abstract class QuickInjectExtension
    {
        private IQuickInjectContainer container;

        private ExtensionContext context;

        public IQuickInjectContainer Container
        {
            get { return this.container; }
        }

        protected ExtensionContext Context
        {
            get { return this.context; }
        }

        internal void InitializeExtension(ExtensionContext extensionContext)
        {
            if (extensionContext == null)
            {
                throw new ArgumentNullException("extensionContext");
            }

            this.container = extensionContext.Container;
            this.context = extensionContext;
            this.Initialize();
        }

        protected abstract void Initialize();
    }
}