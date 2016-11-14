// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;
    using System.Collections.Generic;

    public class PerThreadLifetimeManager : ContextInvariantLifetimeManager
    {
        [ThreadStatic]
        private static Dictionary<Guid, object> values;

        private readonly Guid key;

        public PerThreadLifetimeManager()
        {
            this.key = Guid.NewGuid();
        }

        public override object GetValue()
        {
            EnsureValues();

            object result;
            values.TryGetValue(this.key, out result);
            return result;
        }

        public override void SetValue(object newValue)
        {
            EnsureValues();

            values[this.key] = newValue;
        }

        public override void RemoveValue()
        {
        }

        private static void EnsureValues()
        {
            // no need for locking, values is TLS
            if (values == null)
            {
                values = new Dictionary<Guid, object>();
            }
        }
    }
}