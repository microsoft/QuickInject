// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;

    public abstract class QuickInjectExtension
    {
        private IQuickInjectContainer container;

        private ExtensionContext context;

        public IQuickInjectContainer Container => this.container;

        protected ExtensionContext Context => this.context;

        internal void InitializeExtension(ExtensionContext extensionContext)
        {
            if (extensionContext == null)
            {
                throw new ArgumentNullException(nameof(extensionContext));
            }

            this.container = extensionContext.Container;
            this.context = extensionContext;
            this.Initialize();
        }

        protected abstract void Initialize();
    }
}