// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    public abstract class LifetimeManager
    {
        public abstract object GetValue();

        public abstract object GetValue(object resolutionContext);

        public abstract void SetValue(object newValue);

        public abstract void SetValue(object resolutionContext, object newValue);

        public abstract void RemoveValue();

        public abstract void RemoveValue(object resolutionContext);
    }
}