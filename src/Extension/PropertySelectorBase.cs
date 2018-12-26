// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public abstract class PropertySelectorBase<TResolutionAttribute> : IPropertySelectorPolicy
        where TResolutionAttribute : Attribute
    {
        public IEnumerable<PropertyInfo> GetProperties(Type t)
        {
            foreach (var prop in GetPropertiesHierarchical(t).Where(p => p.CanWrite))
            {
                var propertyMethod = prop.SetMethod ?? prop.GetMethod;
                if (propertyMethod.IsStatic)
                {
                    continue;
                }

                if (prop.GetIndexParameters().Length == 0 && prop.IsDefined(typeof(TResolutionAttribute), false))
                {
                    yield return prop;
                }
            }
        }

        private static IEnumerable<PropertyInfo> GetPropertiesHierarchical(Type type)
        {
            if (type == null)
            {
                return Enumerable.Empty<PropertyInfo>();
            }

            var typeInfo = type.GetTypeInfo();
            return type == typeof(object) ? typeInfo.DeclaredProperties : typeInfo.DeclaredProperties.Concat(GetPropertiesHierarchical(typeInfo.BaseType));
        }
    }
}