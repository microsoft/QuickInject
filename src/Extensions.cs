namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    internal static class Extensions
    {
        public static void AddOrUpdate<K, V>(this Dictionary<K, V> instance, K key, V value)
        {
            if (instance.ContainsKey(key))
            {
                instance[key] = value;
            }
            else
            {
                instance.Add(key, value);
            }
        }

        public static ConstructorInfo GetLongestConstructor(this Type type)
        {
            ConstructorInfo[] ctors = type.GetTypeInfo().DeclaredConstructors.Where(t => t.IsPublic).ToArray();
            ConstructorInfo selectedConstructor = ctors[0];
            for (int i = 1; i < ctors.Length; ++i)
            {
                if (selectedConstructor.GetParameters().Length < ctors[i].GetParameters().Length)
                {
                    selectedConstructor = ctors[i];
                }
            }

            return selectedConstructor;
        }

        public static IEnumerable<Type> ConstructorDependencies(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var ctors = typeInfo.DeclaredConstructors.Where(t => t.IsPublic);
            if (typeInfo.IsInterface || typeInfo.IsAbstract || !ctors.Any())
            {
                throw new Exception("Don't know to how to make " + type);
            }

            var parameters = type.GetLongestConstructor().GetParameters();

            // parameterless
            if (parameters.Length == 0)
            {
                return Enumerable.Empty<Type>();
            }

            return parameters.Select(t => t.ParameterType);
        }

        public static Expression GenExpression(this InjectionMember injectionMember, Type registrationType, IQuickInjectContainer container)
        {
            var injectionMemberType = injectionMember.GetType();
            var baseType = injectionMemberType.GetTypeInfo().BaseType;

            if (baseType == typeof(InjectionMember))
            {
                return new ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(injectionMember);
            }

            throw new Exception("Unknown registration factory");
        }
    }
}