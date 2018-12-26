// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace UnitTests
{
    using Microsoft.QuickInject;

    internal sealed class TestLifetimeManager : LifetimeManager
    {
        private object instance;

        public override object GetValue()
        {
            return this.instance;
        }

        public override object GetValue(object resolutionContext)
        {
            return this.instance;
        }

        public override void SetValue(object newValue)
        {
            this.instance = newValue;
        }

        public override void SetValue(object resolutionContext, object newValue)
        {
            this.instance = newValue;
        }

        public override void RemoveValue()
        {
        }

        public override void RemoveValue(object resolutionContext)
        {
        }
    }
}