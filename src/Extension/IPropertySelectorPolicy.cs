namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface IPropertySelectorPolicy
    {
        IEnumerable<PropertyInfo> GetProperties(Type t);
    }
}