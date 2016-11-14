// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;

    public sealed class RegisterInstanceEventArgs : EventArgs
    {
        public RegisterInstanceEventArgs(Type registeredType, object instance, LifetimeManager lifetimeManager)
        {
            if (registeredType == null)
            {
                throw new ArgumentNullException(nameof(registeredType));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (lifetimeManager == null)
            {
                throw new ArgumentNullException(nameof(lifetimeManager));
            }

            this.RegisteredType = registeredType;
            this.Instance = instance;
            this.LifetimeManager = lifetimeManager;
        }

        public Type RegisteredType { get; private set; }

        public object Instance { get; private set; }

        public LifetimeManager LifetimeManager { get; private set; }
    }
}