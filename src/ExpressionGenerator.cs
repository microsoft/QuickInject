namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    internal sealed class ExpressionGenerator
    {
        private static readonly Type QuickInjectContainerType = typeof(IQuickInjectContainer);
        
        private readonly List<TypeRegistration> registrations;

        private readonly IQuickInjectContainer container;

        private readonly List<ParameterExpression> parameterExpressions = new List<ParameterExpression>();

        private readonly Dictionary<Type, Stack<ParameterExpression>> parameterExpressionsByType = new Dictionary<Type, Stack<ParameterExpression>>();
        
        public ExpressionGenerator(IQuickInjectContainer container, List<TypeRegistration> registrations)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            if (registrations == null)
            {
                throw new ArgumentNullException("registrations");
            }

            this.container = container;
            this.registrations = registrations;
            this.ResolutionContextParameter = Expression.Parameter(typeof(object), "resolutionContextParameter");
            this.SetupLocalVariableExpressions();
        }

        public ParameterExpression ResolutionContextParameter { get; private set; }

        public Expression Generate()
        {
            var body = new List<Expression>();

            for (int i = 0; i < this.registrations.Count; ++i)
            {
                bool finalExpression = this.registrations.Count - 1 == i;
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

                var coreFetchExpression = this.GenerateFetchExpression(variable, registration, finalExpression);
                var lifetimeLookupCall = Expression.Call(Expression.Constant(registration.LifetimeManager, lifetimeType), lifetimeType.GetRuntimeMethods().Single(x => x.Name == "GetValue"));
                var fetchExpression = Expression.Block(coreFetchExpression, this.GenerateSetValueCall(variable, registration), variable);
                var equalsExpression = Expression.Equal(Expression.Assign(variable, Expression.TypeAs(lifetimeLookupCall, registration.RegistrationType)), Expression.Constant(null));

                // last expression is special
                if (finalExpression)
                {
                    body.Add(fetchExpression);
                    return Expression.Block(this.parameterExpressions, Expression.Condition(equalsExpression, Expression.Block(body), variable));
                }

                body.Add(Expression.Condition(equalsExpression, fetchExpression, variable));
            }

            return Expression.Block(this.parameterExpressions, body);
        }

        private Expression GenerateFetchExpression(ParameterExpression variable, TypeRegistration registration, bool finalExpression)
        {
            // Factory case
            if (registration.Factory != null)
            {
                return this.GenerateFactoryExpression(variable, registration);
            }

            /* Non registered IFoo case, we can't throw yet, because it's possible that the lifetime manager will give it to us */
            if (registration.MappedToType.GetTypeInfo().IsAbstract || registration.MappedToType.GetTypeInfo().IsInterface)
            {
                return this.GenerateThrowUnconstructableExpression(registration);
            }

            if (!finalExpression)
            {
                return this.GenerateResolveExpression(this.container, variable, registration);
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

            if (factoryType == typeof(ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression))
            {
                resolvedExpression = this.GenerateParameterizedLambdaExpressionInjectionFactoryMethodCallExpression(registration);
            }
            else if (factoryType == typeof(ResolutionContextParameterExpression))
            {
                resolvedExpression = this.ResolutionContextParameter;
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

        private Expression GenerateResolveExpression(IQuickInjectContainer quickInjectContainer, ParameterExpression variable, TypeRegistration registration)
        {
            Type argumentType = registration.RegistrationType;
            MethodInfo resolve = QuickInjectContainerType.GetRuntimeMethods().Single(x => x.Name == "Resolve" && x.GetParameters().Length == 1);
            var containerResolveT = Expression.Call(Expression.Constant(quickInjectContainer, QuickInjectContainerType), resolve, Expression.Constant(argumentType));
            return Expression.Assign(variable, Expression.Convert(containerResolveT, argumentType));
        }
    }
}