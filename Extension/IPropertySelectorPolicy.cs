// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// The policy that defines how properties that qualify for DI build-up are select from a type.
    /// </summary>
    public interface IPropertySelectorPolicy
    {
        /// <summary>
        /// Gets the properties that qualify for DI build-up.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>The collection of properties.</returns>
        IEnumerable<PropertyInfo> GetProperties(Type t);
    }
}