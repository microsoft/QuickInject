namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.Practices.Unity;

    internal sealed class ExpressionGenerator
    {
        private static readonly Type UnityContainerType = typeof(IUnityContainer);

        private static readonly ResolverOverride[] EmptyResolverOverridesArray = { };

        private readonly List<TypeRegistration> registrations;

        private readonly IUnityContainer container;

        private readonly List<ParameterExpression> parameterExpressions = new List<ParameterExpression>();

        private readonly Dictionary<Type, Stack<ParameterExpression>> parameterExpressionsByType = new Dictionary<Type, Stack<ParameterExpression>>();

        public ExpressionGenerator(IUnityContainer container, List<TypeRegistration> registrations)
        {
            this.container = container;
            this.registrations = registrations;
            this.SetupLocalVariableExpressions();
        }

        public Expression Generate()
        {
            var body = new List<Expression>();

            for (int i = 0; i < this.registrations.Count; ++i)
            {
                ParameterExpression variable = this.parameterExpressions[i];
                TypeRegistration registration = this.registrations[i];
                Type type = registration.RegistrationType;
                Type lifetimeType = registration.LifetimeManager.GetType();

                if (this.parameterExpressionsByType.ContainsKey(type))
                {
                    this.parameterExpressionsByType[type].Push(variable);
                }
                else
                {
                    var s = new Stack<ParameterExpression>();
                    s.Push(variable);
                    this.parameterExpressionsByType.Add(type, s);
                }

                var coreFetchExpression = this.GenerateFetchExpression(variable, registration);
                var lifetimeLookupCall = Expression.Call(Expression.Constant(registration.LifetimeManager, lifetimeType), lifetimeType.GetRuntimeMethods().Single(x => x.Name == "GetValue"));
                var fetchExpression = Expression.Block(coreFetchExpression, this.GenerateSetValueCall(variable, registration), variable);
                var equalsExpression = Expression.Equal(Expression.Assign(variable, Expression.TypeAs(lifetimeLookupCall, registration.RegistrationType)), Expression.Constant(null));

                // last expression is special
                if (this.registrations.Count - 1 == i)
                {
                    body.Add(fetchExpression);
                    return Expression.Block(this.parameterExpressions, Expression.Condition(equalsExpression, Expression.Block(body), variable));
                }

                body.Add(Expression.Condition(equalsExpression, fetchExpression, variable));
            }

            return Expression.Block(this.parameterExpressions, body);
        }

        private Expression GenerateFetchExpression(ParameterExpression variable, TypeRegistration registration)
        {
            // Factory case
            if (registration.Factory != null)
            {
                return this.GenerateFactoryExpression(variable, registration);
            }

            /* Func<T>, similar to factory methods, but we generate the Func expression as well */
            if (registration.RegistrationType.GetTypeInfo().IsGenericType && registration.RegistrationType.GetGenericTypeDefinition().GetTypeInfo().BaseType == typeof(MulticastDelegate))
            {
                return this.GenerateFuncTExpression(this.container, variable, registration);
            }

            /* Non registered IFoo case, we can't throw yet, because it's possible that the lifetime manager will give it to us */
            if (registration.MappedToType.GetTypeInfo().IsAbstract || registration.MappedToType.GetTypeInfo().IsInterface)
            {
                return this.GenerateThrowUnconstructableExpression(registration);
            }

            /* new() case and new(param ...) case */
            return this.GenerateExpressionForParameterConstructor(variable, registration);
        }

        private Expression GenerateThrowUnconstructableExpression(TypeRegistration registration)
        {
            return
                Expression.Throw(
                    Expression.Constant(
                        new ArgumentException(
                            string.Format(
                                "Attempted to construct an interface or abstract class of Type \""
                                + registration.MappedToType + "\""))));
        }

        private Expression GenerateExpressionForParameterConstructor(ParameterExpression variable, TypeRegistration typeRegistration)
        {
            ConstructorInfo constructor = typeRegistration.MappedToType.GetLongestConstructor();
            var ctorParams = constructor.GetParameters();
            if (ctorParams.Length == 0)
            {
                return Expression.Assign(variable, Expression.New(constructor));
            }

            return Expression.Assign(variable, Expression.New(constructor, ctorParams.Select(ctorParam => this.parameterExpressionsByType[ctorParam.ParameterType].Pop())));
        }

        private Expression GenerateFactoryExpression(ParameterExpression variable, TypeRegistration registration)
        {
            Expression resolvedExpression;
            var factoryType = registration.Factory.GetType();

            if (factoryType == typeof(ParameterizedInjectionFactoryMethodCallExpression))
            {
                resolvedExpression = this.GenerateParameterizedInjectionFactoryMethodCallExpression(registration);
            }
            else if (factoryType == typeof(ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression))
            {
                resolvedExpression = this.GenerateParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(registration);
            }
            else if (factoryType == typeof(InjectionFactoryMethodCallExpression))
            {
                resolvedExpression = this.GenerateInjectionFactoryMethodCallExpression(registration);
            }
            else
            {
                resolvedExpression = registration.Factory;
            }

            return Expression.Assign(variable, Expression.TypeAs(resolvedExpression, registration.RegistrationType));
        }

        private Expression GenerateParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(TypeRegistration registration)
        {
            var parameterizedFactory = (ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression)registration.Factory;
            return parameterizedFactory.Resolve(this.parameterExpressionsByType);
        }

        private Expression GenerateParameterizedInjectionFactoryMethodCallExpression(TypeRegistration registration)
        {
            var parameterizedFactory = (ParameterizedInjectionFactoryMethodCallExpression)registration.Factory;
            return parameterizedFactory.Resolve(this.parameterExpressionsByType);
        }

        private Expression GenerateInjectionFactoryMethodCallExpression(TypeRegistration registration)
        {
            var injectionFactory = (InjectionFactoryMethodCallExpression)registration.Factory;
            return injectionFactory.Resolve(this.container);
        }

        private Expression GenerateFuncTExpression(IUnityContainer unityContainer, ParameterExpression variable, TypeRegistration registration)
        {
            Type type = registration.RegistrationType;
            var argumentType = type.GenericTypeArguments[0];
            MethodInfo resolve = UnityContainerType.GetRuntimeMethods().Single(x => x.Name == "Resolve");
            var containerResolveT = Expression.Call(Expression.Constant(unityContainer, UnityContainerType), resolve, new Expression[] { Expression.Constant(argumentType), Expression.Constant(string.Empty), Expression.Constant(EmptyResolverOverridesArray) });
            var lambdaExpr = Expression.Lambda(type, Expression.Convert(containerResolveT, argumentType));
            return Expression.Assign(variable, lambdaExpr);
        }

        private Expression GenerateSetValueCall(ParameterExpression variable, TypeRegistration registration)
        {
            LifetimeManager lifetimeManager = registration.LifetimeManager;
            return Expression.Call(Expression.Constant(lifetimeManager), lifetimeManager.GetType().GetRuntimeMethods().Single(x => x.Name == "SetValue"), new Expression[] { variable });
        }

        private void SetupLocalVariableExpressions()
        {
            foreach (TypeRegistration registration in this.registrations)
            {
                Type type = registration.RegistrationType;

                ParameterExpression localVariable = Expression.Variable(type);
                this.parameterExpressions.Add(localVariable);
            }
        }
    }
}