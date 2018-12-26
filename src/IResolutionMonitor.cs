// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;

    public interface IResolutionMonitor
    {
        void Begin(Type t, object resolutionContext);

        void End(Type t, object resolutionContext);
    }
}