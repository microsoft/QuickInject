// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
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
            foreach (PropertyInfo prop in GetPropertiesHierarchical(t).Where(p => p.CanWrite))
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

            if (type.Equals(typeof(object)))
            {
                return type.GetTypeInfo().DeclaredProperties;
            }

            return type.GetTypeInfo().DeclaredProperties.Concat(GetPropertiesHierarchical(type.GetTypeInfo().BaseType));
        }
    }
}