namespace QuickInject
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.Practices.Unity;

    public class QuickInjectContainer : IQuickInjectContainer, IUnityContainer, IServiceProvider
    {
        private static readonly Type UnityContainerType = typeof(IUnityContainer);

        private static readonly ResolverOverride[] NoResolverOverrides = { };

        private static readonly QuickInjectEventSource Logger = new QuickInjectEventSource();

        private readonly object lockObj = new object();

        private readonly QuickInjectContainer parentContainer;

        private readonly Dictionary<Type, TypeRegistration> registrationTable = new Dictionary<Type, TypeRegistration>();

        private readonly Dictionary<Type, List<Type>> typeReverseIndex = new Dictionary<Type, List<Type>>();

        private readonly Dictionary<Type, BuildPlan> buildPlanTable = new Dictionary<Type, BuildPlan>();

        private readonly Dictionary<Type, BuildPlan> slowBuildPlanTable = new Dictionary<Type, BuildPlan>();

        private readonly Dictionary<Type, LifetimeManager> fallbackLifetimeTable = new Dictionary<Type, LifetimeManager>();

        private readonly List<IBuildPlanVisitor> buildPlanVisitors = new List<IBuildPlanVisitor>();

        private readonly ConcurrentDictionary<Type, PropertyInfo[]> propertyInfoTable = new ConcurrentDictionary<Type, PropertyInfo[]>();

        private readonly ExtensionImpl extensionImpl;

        private Action<ITreeNode<Type>> dependencyTreeListener;

        public QuickInjectContainer()
        {
            this.extensionImpl = new ExtensionImpl(this, new DummyPolicyList());
            this.buildPlanTable.Add(UnityContainerType, new BuildPlan { IsCompiled = false });
            this.registrationTable.Add(UnityContainerType, new TypeRegistration(UnityContainerType, UnityContainerType, new ContainerControlledLifetimeManager(), Expression.Constant(this)));

            this.Registering += delegate { };
            this.RegisteringInstance += delegate { };
            this.ChildContainerCreated += delegate { };
        }

        private QuickInjectContainer(QuickInjectContainer parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            this.Registering += delegate { };
            this.RegisteringInstance += delegate { };
            this.ChildContainerCreated += delegate { };

            this.RegisterDependencyTreeListener(parent.dependencyTreeListener);
            foreach (var visitor in parent.buildPlanVisitors)
            {
                this.AddBuildPlanVisitor(visitor);
            }

            this.parentContainer = parent;
            this.extensionImpl = this.parentContainer.extensionImpl;
            this.buildPlanTable.Add(UnityContainerType, new BuildPlan { IsCompiled = false });
            this.registrationTable.Add(UnityContainerType, new TypeRegistration(UnityContainerType, UnityContainerType, new ContainerControlledLifetimeManager(), Expression.Constant(this)));
        }

        internal event EventHandler<RegisterEventArgs> Registering;

        internal event EventHandler<RegisterInstanceEventArgs> RegisteringInstance;

        internal event EventHandler<ChildContainerCreatedEventArgs> ChildContainerCreated;

        public IUnityContainer Parent
        {
            get
            {
                return this.parentContainer;
            }
        }

        public IEnumerable<ContainerRegistration> Registrations
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public IUnityContainer AddExtension(UnityContainerExtension extension)
        {
            extension.InitializeExtension(this.extensionImpl);
            return this;
        }

        public object BuildUp(Type t, object existing, string name, params ResolverOverride[] resolverOverrides)
        {
            PropertyInfo[] propertyInfos;
            if (this.propertyInfoTable.TryGetValue(t, out propertyInfos))
            {
                foreach (PropertyInfo p in propertyInfos)
                {
                    p.SetValue(existing, this.Resolve(p.PropertyType, null, NoResolverOverrides));
                }
            }
            else
            {
                this.SlowBuildUp(existing, t);
            }

            return existing;
        }

        public object Configure(Type configurationInterface)
        {
            throw new NotSupportedException();
        }

        public IUnityContainer CreateChildContainer()
        {
            var child = new QuickInjectContainer(this);
            var childContext = new ExtensionImpl(child, new DummyPolicyList());
            this.ChildContainerCreated(this, new ChildContainerCreatedEventArgs(childContext));
            return child;
        }

        public IUnityContainer RegisterInstance(Type t, string name, object instance, LifetimeManager lifetime)
        {
            if (!string.IsNullOrEmpty(name))
            {
                throw new NotSupportedException("Named registrations are not supported");
            }

            if (lifetime == null)
            {
                throw new NotSupportedException("Lifetime for instance registrations is required");
            }

            Expression expression = Expression.Constant(instance);

            this.RegisteringInstance(this, new RegisterInstanceEventArgs(t, instance, name, lifetime));

            lock (this.lockObj)
            {
                var registration = new TypeRegistration(t, t, lifetime, expression);

                if (this.registrationTable.ContainsKey(t))
                {
                    var existingRegistration = this.registrationTable[t];
                    existingRegistration.LifetimeManager = lifetime;
                    existingRegistration.Factory = expression;
                    registration = existingRegistration;
                }
                else
                {
                    this.registrationTable.Add(t, registration);
                }

                if (this.typeReverseIndex.ContainsKey(t))
                {
                    // fix up all the entries previously pointing to this "registrationType"
                    foreach (var x in this.typeReverseIndex[t])
                    {
                        this.registrationTable[x].Factory = expression;
                        this.registrationTable[x].LifetimeManager = registration.LifetimeManager;
                    }
                }

                registration.LifetimeManager.SetValue(instance);

                var plan = new BuildPlan { IsCompiled = false };
                if (this.buildPlanTable.ContainsKey(t))
                {
                    this.buildPlanTable[t] = plan;
                }
                else
                {
                    this.buildPlanTable.Add(t, plan);
                }
            }

            return this;
        }

        public IUnityContainer RegisterType(Type from, Type to, string name, LifetimeManager lifetimeManager, params InjectionMember[] injectionMembers)
        {
            if ((from != null && from.GetTypeInfo().IsGenericTypeDefinition) || to.GetTypeInfo().IsGenericTypeDefinition)
            {
                throw new ArgumentException("Open Generic Types are not supported");
            }

            if (!string.IsNullOrEmpty(name))
            {
                throw new NotSupportedException("Named registrations are not supported");
            }

            if (injectionMembers.Length > 1)
            {
                throw new NotSupportedException("Multiple injection members are not supported");
            }

            Type registrationType = from ?? to;
            var registration = new TypeRegistration(registrationType, to, lifetimeManager, injectionMembers.Length == 1 ? injectionMembers[0].GenExpression(registrationType, this) : null);

            Logger.RegisterType(registrationType, to, lifetimeManager);
            this.Registering(this, new RegisterEventArgs(from, to, name, lifetimeManager));

            lock (this.lockObj)
            {
                if (this.registrationTable.ContainsKey(registrationType))
                {
                    var existingRegistration = this.registrationTable[registrationType];

                    existingRegistration.MappedToType = to;

                    if (lifetimeManager != null)
                    {
                        existingRegistration.LifetimeManager = lifetimeManager;
                    }

                    if (injectionMembers.Length == 1)
                    {
                        existingRegistration.Factory = registration.Factory;
                    }

                    registration = existingRegistration;
                }
                else
                {
                    this.registrationTable.Add(registrationType, registration);
                }

                if (registration.Factory == null)
                {
                    registration.MappedToType = this.MostSignificantMappedToType(registration.MappedToType);
                    registration.Factory = this.FactoryForType(registration.MappedToType);
                }

                if (registration.LifetimeManager != null)
                {
                    if (this.fallbackLifetimeTable.ContainsKey(registration.MappedToType))
                    {
                        this.fallbackLifetimeTable[registration.MappedToType] = registration.LifetimeManager;
                    }
                    else
                    {
                        this.fallbackLifetimeTable.Add(registration.MappedToType, registration.LifetimeManager);
                    }
                }

                // build reverse index if from and to are different types
                if (from != null && from != to)
                {
                    if (this.typeReverseIndex.ContainsKey(to))
                    {
                        this.typeReverseIndex[to].Add(from);
                    }
                    else
                    {
                        this.typeReverseIndex.Add(to, new List<Type> { from });
                    }
                }

                // lifetimes
                if (registration.LifetimeManager != null)
                {
                    if (this.typeReverseIndex.ContainsKey(registrationType))
                    {
                        // fix up all the entries previously pointing to this "registrationType"
                        foreach (var x in this.typeReverseIndex[registrationType])
                        {
                            this.registrationTable[x].LifetimeManager = registration.LifetimeManager;
                        }
                    }
                }

                // Being asked to register as a factory
                if (injectionMembers.Length == 1)
                {
                    if (this.typeReverseIndex.ContainsKey(registrationType))
                    {
                        // fix up all the entries previously pointing to this "registrationType"
                        foreach (var x in this.typeReverseIndex[registrationType])
                        {
                            this.registrationTable[x].Factory = registration.Factory;
                        }
                    }
                }
                else
                {
                    // fix up all the mappings of previously registered types
                    if (from != null && from != to)
                    {
                        if (this.typeReverseIndex.ContainsKey(from))
                        {
                            foreach (var x in this.typeReverseIndex[from])
                            {
                                this.registrationTable[x].MappedToType = to;
                            }
                        }
                    }
                }

                var plan = new BuildPlan { IsCompiled = false };
                if (this.buildPlanTable.ContainsKey(registrationType))
                {
                    this.buildPlanTable[registrationType] = plan;
                }
                else
                {
                    this.buildPlanTable.Add(registrationType, plan);
                }
            }

            return this;
        }

        public IUnityContainer RemoveAllExtensions()
        {
            throw new NotSupportedException();
        }

        public object Resolve(Type t, string name, params ResolverOverride[] resolverOverrides)
        {
            BuildPlan plan;
            if (this.buildPlanTable.TryGetValue(t, out plan) && plan.IsCompiled)
            {
#if DEBUG
                if (Logger.IsEnabled())
                {
                    Logger.FastResolve(t.ToString());
                }
#endif
                return plan.Expression();
            }

            return this.SlowResolve(t, plan == null)();
        }

        public IEnumerable<object> ResolveAll(Type t, params ResolverOverride[] resolverOverrides)
        {
            throw new NotSupportedException();
        }

        public void Teardown(object o)
        {
        }

        public void Dispose()
        {
        }

        public object GetService(Type serviceType)
        {
            return this.Resolve(serviceType, null, NoResolverOverrides);
        }

        public void AddBuildPlanVisitor(IBuildPlanVisitor visitor)
        {
            lock (this.lockObj)
            {
                this.buildPlanVisitors.Add(visitor);
            }
        }

        public void RegisterDependencyTreeListener(Action<ITreeNode<Type>> action)
        {
            this.dependencyTreeListener = action;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Func<object> CompilePlan(Type t, Dictionary<Type, BuildPlan> planTable, bool slowPath)
        {
            var depTree = t.BuildDependencyTree(this.Dependencies);
            var topologicalOrdering = depTree.Sort(node => node.Children).Select(x => x.Value);
            IEnumerable<Type> types = topologicalOrdering;

            var typeRegistrations = new List<TypeRegistration>();
            foreach (var type in types)
            {
                var registration = this.GetRegistration(type);
                if (registration == null)
                {
#if DEBUG
                    if (Logger.IsEnabled())
                    {
                        Logger.UnregisteredResolve(type.ToString());
                    }
#endif
                    typeRegistrations.Add(new TypeRegistration(type, type, this.GetFallbackLifetimeManager(type), null));
                }
                else
                {
                    if (registration.LifetimeManager == null)
                    {
                        typeRegistrations.Add(new TypeRegistration(registration.RegistrationType, registration.MappedToType, new TransientLifetimeManager(), registration.Factory));
                    }
                    else
                    {
                        typeRegistrations.Add(registration);    
                    }
                }
            }

            var codeGenerator = new ExpressionGenerator(this, typeRegistrations);
            var eptree = codeGenerator.Generate();

            if (this.dependencyTreeListener != null)
            {
                this.dependencyTreeListener(depTree);
            }

            eptree = this.buildPlanVisitors.Aggregate(eptree, (current, visitor) => visitor.Visitor(current, t, slowPath));

            var compiledexpression = Expression.Lambda<Func<object>>(eptree, "Create_" + t, null).Compile();

            BuildPlan plan;
            if (planTable.TryGetValue(t, out plan))
            {
                plan.IsCompiled = true;
                plan.Expression = compiledexpression;
            }
            else
            {
                planTable.Add(t, new BuildPlan { IsCompiled = true, Expression = compiledexpression });
            }

            return compiledexpression;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Func<object> SlowResolve(Type t, bool slow)
        {
            Func<object> returnVal;

            lock (this.lockObj)
            {
                if (slow)
                {
#if DEBUG
                    if (Logger.IsEnabled())
                    {
                        Logger.SlowResolve(t.ToString());
                    }
#endif

                    BuildPlan slowPlan;
                    returnVal = this.slowBuildPlanTable.TryGetValue(t, out slowPlan) ? slowPlan.Expression : this.CompilePlan(t, this.slowBuildPlanTable, true);
                }
                else
                {
                    BuildPlan plan = this.buildPlanTable[t];
                    returnVal = plan.IsCompiled ? plan.Expression : this.CompilePlan(t, this.buildPlanTable, false);
                }
            }

            return returnVal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SlowBuildUp(object existing, Type t)
        {
            var context = new DummyBuilderContext { BuildKey = new NamedTypeBuildKey(t) };
            var policyList = (DummyPolicyList)this.extensionImpl.Policies;

            if (policyList.PropertySelectorPolicy != null)
            {
                var selectedProperties = policyList.PropertySelectorPolicy.SelectProperties(context, policyList).Select(x => x.Property).ToArray();
                this.propertyInfoTable.TryAdd(t, selectedProperties);

                foreach (var selectedProperty in selectedProperties)
                {
                    selectedProperty.SetValue(existing, this.Resolve(selectedProperty.PropertyType, null, NoResolverOverrides));
                }
            }
        }

        private IEnumerable<Type> Dependencies(Type type)
        {
            if (this.registrationTable.ContainsKey(type))
            {
                var registration = this.registrationTable[type];

                // container.RegisterType<IFoo>(new *InjectionFactory(...))
                if (registration.Factory != null)
                {
                    // container.RegisterType<IFoo>(new InjectionFactory(...))
                    if (registration.Factory.GetType() == typeof(ParameterizedInjectionFactoryMethodCallExpression))
                    {
                        return ((ParameterizedInjectionFactoryMethodCallExpression)registration.Factory).DependentTypes;
                    }

                    // container.RegisterType<IFoo>(new ParameterizedInjectionFactory<IProvider1, IProvider2, IFoo>(...))
                    if (registration.Factory.GetType() == typeof(ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression))
                    {
                        return ((ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression)registration.Factory).DependentTypes;
                    }

                    // Opaque, has no dependencies
                    return Enumerable.Empty<Type>();
                }

                // container.RegisterType<IFoo, Foo>();
                if (registration.RegistrationType != registration.MappedToType)
                {
                    return this.Dependencies(registration.MappedToType);
                }

                // container.RegisterType<IFoo>(new LifetimeManagerWillProvideValue())
                if (type.GetTypeInfo().IsInterface || type.GetTypeInfo().IsAbstract)
                {
                    return Enumerable.Empty<Type>();
                }

                // container.RegisterType<Foo>();
                return type.ConstructorDependencies();
            }

            // crawl parent containers
            while (this.parentContainer != null)
            {
                return this.parentContainer.Dependencies(type);
            }

            // Special Case: Func<T> that is not registered
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
            {
                return Enumerable.Empty<Type>();
            }

            // Regular class that can be constructed
            return type.ConstructorDependencies();
        }

        private LifetimeManager GetFallbackLifetimeManager(Type type)
        {
            if (this.fallbackLifetimeTable.ContainsKey(type))
            {
                return this.fallbackLifetimeTable[type];
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetFallbackLifetimeManager(type);
            }

            return new TransientLifetimeManager();
        }

        private TypeRegistration GetRegistration(Type type)
        {
            if (this.registrationTable.ContainsKey(type))
            {
                return this.registrationTable[type];
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetRegistration(type);
            }

            return null;
        }

        private Type MostSignificantMappedToType(Type type)
        {
            if (this.registrationTable.ContainsKey(type))
            {
                Type mappedType = this.registrationTable[type].MappedToType;
                if (type != mappedType)
                {
                    return this.MostSignificantMappedToType(this.registrationTable[mappedType].MappedToType);
                }
            }

            return type;
        }

        private Expression FactoryForType(Type type)
        {
            if (this.registrationTable.ContainsKey(type))
            {
                return this.registrationTable[type].Factory;
            }

            return null;
        }
    }
}