// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    public sealed class TransientLifetimeManager : ContextInvariantLifetimeManager
    {
        public override object GetValue()
        {
            return null;
        }

        public override void SetValue(object newValue)
        {
        }

        public override void RemoveValue()
        {
        }
    }
}