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

        public static IEnumerable<T> Sort<T>(this T source, Func<T, IEnumerable<T>> dependencies)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            Visit(source, visited, sorted, dependencies);

            return sorted;
        }

        public static ITreeNode<TypeRegistration> BuildDependencyTree(this Type value, Func<Type, IEnumerable<Type>> dependencywalker, Func<Type, TypeRegistration> typeRegistrationResolver)
        {
            var stack = new Stack<ITreeNode<Type>>();
            var tree = new TreeNode<Type>(value);

            var registrationStack = new Stack<ITreeNode<TypeRegistration>>();
            var registrationTree = new TreeNode<TypeRegistration>(typeRegistrationResolver(value));

            stack.Push(tree);
            registrationStack.Push(registrationTree);

            while (stack.Count != 0)
            {
                var node = stack.Pop();
                var registrationNode = registrationStack.Pop();

                var dependencies = dependencywalker(node.Value);

                foreach (var dependency in dependencies)
                {
                    stack.Push(node.AddChild(dependency));
                    registrationStack.Push(registrationNode.AddChild(typeRegistrationResolver(dependency)));
                }
            }

            return registrationTree;
        }

        public static Action<object, object> CreatePropertySetter(Type targetType, PropertyInfo property)
        {
            var target = Expression.Parameter(typeof(object), "obj");
            var value = Expression.Parameter(typeof(object), "value");
            var body = Expression.Assign(Expression.Property(Expression.Convert(target, property.DeclaringType), property), Expression.Convert(value, property.PropertyType));
            var lambda = Expression.Lambda<Action<object, object>>(body, target, value);
            return lambda.Compile();
        }

        public static Expression GenExpression(this InjectionMember injectionMember, Type registrationType, IQuickInjectContainer container)
        {
            var injectionMemberType = injectionMember.GetType();
            var baseType = injectionMemberType.GetTypeInfo().BaseType;

            if (baseType == typeof(InjectionMember))
            {
                return new ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(injectionMember.Factory);
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