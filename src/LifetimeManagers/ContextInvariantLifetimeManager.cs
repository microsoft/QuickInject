// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    public abstract class ContextInvariantLifetimeManager : LifetimeManager
    {
        public override object GetValue(object resolutionContext)
        {
            return this.GetValue();
        }

        public override void SetValue(object resolutionContext, object newValue)
        {
            this.SetValue(newValue);
        }

        public override void RemoveValue(object resolutionContext)
        {
            this.RemoveValue();
        }
    }
}