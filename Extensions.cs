namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.Practices.Unity;

    internal static class Extensions
    {
        private static readonly FieldInfo InjectionFactoryFieldInfo = typeof(Microsoft.Practices.Unity.InjectionFactory).GetTypeInfo().GetDeclaredField("factoryFunc");

        public static void AddOrPush<K, V>(this Dictionary<K, Stack<V>> instance, K key, V value)
        {
            if (instance.ContainsKey(key))
            {
                instance[key].Push(value);
            }
            else
            {
                var s = new Stack<V>();
                s.Push(value);
                instance.Add(key, s);
            }
        }

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

        public static IEnumerable<T> Sort<T>(this T source, Func<T, IEnumerable<T>> dependencies)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            Visit(source, visited, sorted, dependencies);

            return sorted;
        }

        public static ITreeNode<T> BuildDependencyTree<T>(this T value, Func<T, IEnumerable<T>> dependencywalker)
        {
            var stack = new Stack<ITreeNode<T>>();
            var tree = new TreeNode<T>(value);

            stack.Push(tree);

            while (stack.Count != 0)
            {
                var node = stack.Pop();

                var dependencies = dependencywalker(node.Value);

                foreach (var dependency in dependencies)
                {
                    stack.Push(node.AddChild(dependency));
                }
            }

            return tree;
        }

        public static Action<object, object> CreatePropertySetter(Type targetType, PropertyInfo property)
        {
            var target = Expression.Parameter(typeof(object), "obj");
            var value = Expression.Parameter(typeof(object), "value");
            var body = Expression.Assign(Expression.Property(Expression.Convert(target, property.DeclaringType), property), Expression.Convert(value, property.PropertyType));
            var lambda = Expression.Lambda<Action<object, object>>(body, target, value);
            return lambda.Compile();
        }

        public static Expression GenExpression(this InjectionMember injectionMember, Type registrationType, IUnityContainer container)
        {
            var injectionMemberType = injectionMember.GetType();
            var baseType = injectionMemberType.GetTypeInfo().BaseType;

            if (baseType == typeof(ParameterizedLambdaExpressionInjectionFactoryBase))
            {
                var factory = (ParameterizedLambdaExpressionInjectionFactoryBase)injectionMember;
                return new ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(factory.Factory);
            }

            if (baseType == typeof(ParameterizedInjectionFactoryBase))
            {
                var parameterizedFactory = (ParameterizedInjectionFactoryBase)injectionMember;
                return new ParameterizedInjectionFactoryMethodCallExpression(parameterizedFactory.Factory.Target, parameterizedFactory.Factory.GetMethodInfo());
            }

            if (baseType == typeof(ServiceProviderInjectionFactoryBase))
            {
                return new InjectionFactoryMethodCallExpression(((ServiceProviderInjectionFactoryBase)injectionMember).Factory);
            }

            if (injectionMemberType == typeof(Microsoft.Practices.Unity.InjectionFactory))
            {
                var parameters = new Expression[] { Expression.Constant(container), Expression.Constant(registrationType), Expression.Constant(string.Empty) };
                var func = (Delegate)InjectionFactoryFieldInfo.GetValue(injectionMember);
                var expr = func.Target == null ? Expression.Call(func.GetMethodInfo(), parameters) : Expression.Call(Expression.Constant(func.Target), func.GetMethodInfo(), parameters);
                return new InjectionFactoryMethodCallExpression(expr);
            }

            throw new Exception("Unknown registration factory");
        }

        private static void Visit<T>(T item, HashSet<T> visited, List<T> sorted, Func<T, IEnumerable<T>> dependencies)
        {
            if (!visited.Contains(item))
            {
                visited.Add(item);

                foreach (var dep in dependencies(item))
                {
                    Visit(dep, visited, sorted, dependencies);
                }

                sorted.Add(item);
            }
            else
            {
                throw new CyclicDependencyException<T>(item);
            }
        }
    }
}