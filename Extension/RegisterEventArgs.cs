// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;

    public sealed class RegisterEventArgs : EventArgs
    {
        public RegisterEventArgs(Type typeFrom, Type typeTo, LifetimeManager lifetimeManager)
        {
            if (typeTo == null)
            {
                throw new ArgumentNullException(nameof(typeTo));
            }

            this.TypeFrom = typeFrom;
            this.TypeTo = typeTo;
            this.LifetimeManager = lifetimeManager;
        }

        public Type TypeFrom { get; private set; }

        public Type TypeTo { get; private set; }

        public LifetimeManager LifetimeManager { get; private set; }
    }
}