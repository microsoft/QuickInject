// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;

    public abstract class ExtensionContext
    {
        public abstract event EventHandler<RegisterEventArgs> Registering;

        public abstract event EventHandler<RegisterInstanceEventArgs> RegisteringInstance;

        public abstract event EventHandler<ChildContainerCreatedEventArgs> ChildContainerCreated;

        public abstract IQuickInjectContainer Container { get; }
    }
}