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

        private readonly ITreeNode<TypeRegistration> registrations;

        private readonly IUnityContainer container;

        private readonly List<ParameterExpression> parameterExpressions = new List<ParameterExpression>();

        private readonly Dictionary<Type, Stack<ParameterExpression>> parameterExpressionsByType = new Dictionary<Type, Stack<ParameterExpression>>();

        private readonly Stack<Expression> codeExpressions = new Stack<Expression>(); 

        public ExpressionGenerator(IUnityContainer container, ITreeNode<TypeRegistration> registrations)
        {
            this.container = container;
            this.registrations = registrations;
        }

        public Expression Generate()
        {
            this.registrations.PostOrderTraverse(this.VisitCodeGeneration);
            return Expression.Block(this.parameterExpressions, this.codeExpressions.Pop());
        }

        private void VisitCodeGeneration(TypeRegistration registration)
        {
            Type type = registration.RegistrationType;
            Type lifetimeType = registration.LifetimeManager.GetType();
            ParameterExpression variable = Expression.Variable(type);

            this.parameterExpressionsByType.AddOrPush(type, variable);
            this.parameterExpressions.Add(variable);

            var coreFetchExpression = this.GenerateFetchExpression(variable, registration);
            var lifetimeLookupCall = Expression.Call(Expression.Constant(registration.LifetimeManager, lifetimeType), lifetimeType.GetRuntimeMethods().Single(x => x.Name == "GetValue"));
            var fetchExpression = Expression.Block(coreFetchExpression, this.GenerateSetValueCall(variable, registration), variable);
            var equalsExpression = Expression.Equal(Expression.Assign(variable, Expression.TypeAs(lifetimeLookupCall, registration.RegistrationType)), Expression.Constant(null));
            var conditionalExpression = Expression.Condition(equalsExpression, fetchExpression, variable);

            this.codeExpressions.Push(conditionalExpression);
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

            List<Expression> body = new List<Expression>();
            List<ParameterExpression> variables = new List<ParameterExpression>();
            foreach (var ctorParam in ctorParams)
            {
                variables.Add(this.parameterExpressionsByType[ctorParam.ParameterType].Pop());
                body.Add(this.codeExpressions.Pop());
            }

            body.Add(Expression.Assign(variable, Expression.New(constructor, variables)));

            return Expression.Block(body);
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
    }
}