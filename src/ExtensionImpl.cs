// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;

    internal sealed class ExtensionImpl : ExtensionContext
    {
        private readonly QuickInjectContainer container;

        public ExtensionImpl(QuickInjectContainer container)
        {
            this.container = container;
        }

        public override event EventHandler<RegisterEventArgs> Registering
        {
            add => this.container.Registering += value;
            remove => this.container.Registering -= value;
        }

        public override event EventHandler<RegisterInstanceEventArgs> RegisteringInstance
        {
            add => this.container.RegisteringInstance += value;
            remove => this.container.RegisteringInstance -= value;
        }

        public override event EventHandler<ChildContainerCreatedEventArgs> ChildContainerCreated
        {
            add => this.container.ChildContainerCreated += value;
            remove => this.container.ChildContainerCreated -= value;
        }

        public override IQuickInjectContainer Container => this.container;
    }
}