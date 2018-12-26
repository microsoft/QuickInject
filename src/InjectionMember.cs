// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;
    using System.Collections.Generic;

    public abstract class InjectionMember
    {
        public Type ResultType { get; protected set; }

        public IEnumerable<Type> DependentTypes { get; protected set; }

        public ICodeProvider CodeProvider { get; protected set; }
    }
}