// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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