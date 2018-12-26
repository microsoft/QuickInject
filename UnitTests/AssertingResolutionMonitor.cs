// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace UnitTests
{
    using System;
    using Microsoft.QuickInject;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    internal sealed class AssertingResolutionMonitor : IResolutionMonitor
    {
        private readonly Type type;

        public AssertingResolutionMonitor(Type t)
        {
            this.type = t;
        }

        public bool BeginWasCalled { get; private set; }

        public bool EndWasCalled { get; private set; }

        public void Begin(Type t, object resolutionContext)
        {
            this.BeginWasCalled = true;
            Assert.AreEqual(this.type, t);
        }

        public void End(Type t, object resolutionContext)
        {
            this.EndWasCalled = true;
            Assert.AreEqual(this.type, t);
        }
    }
}