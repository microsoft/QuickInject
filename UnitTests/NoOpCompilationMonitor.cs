// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace UnitTests
{
    using System;
    using Microsoft.QuickInject;

    internal sealed class NoOpCompilationMonitor : ICompilationMonitor
    {
        public void Begin(Type t, object resolutionContext)
        {
        }

        public void End(Type t, object resolutionContext)
        {
        }
    }
}