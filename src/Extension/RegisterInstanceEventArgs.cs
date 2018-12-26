// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;

    public sealed class RegisterInstanceEventArgs : EventArgs
    {
        public RegisterInstanceEventArgs(Type registeredType, object instance, LifetimeManager lifetimeManager)
        {
            this.RegisteredType = registeredType ?? throw new ArgumentNullException(nameof(registeredType));
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.LifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
        }

        public Type RegisteredType { get; }

        public object Instance { get; }

        public LifetimeManager LifetimeManager { get; }
    }
}