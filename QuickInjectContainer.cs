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

        private readonly Dictionary<Type, Type> typeMappingTable = new Dictionary<Type, Type>();

        private readonly Dictionary<Type, LifetimeManager> lifetimeTable = new Dictionary<Type, LifetimeManager>();

        private readonly Dictionary<Type, Expression> factoryExpressionTable = new Dictionary<Type, Expression>();

        private readonly Dictionary<Type, BuildPlan> buildPlanTable = new Dictionary<Type, BuildPlan>();

        private readonly Dictionary<Type, BuildPlan> slowBuildPlanTable = new Dictionary<Type, BuildPlan>();

        private readonly List<IBuildPlanVisitor> buildPlanVisitors = new List<IBuildPlanVisitor>();

        private readonly ConcurrentDictionary<Type, PropertyInfo[]> propertyInfoTable = new ConcurrentDictionary<Type, PropertyInfo[]>();

        private readonly ExtensionImpl extensionImpl;

        private Action<ITreeNode<Type>> dependencyTreeListener;

        public QuickInjectContainer()
        {
            this.extensionImpl = new ExtensionImpl(this, new DummyPolicyList());
            this.buildPlanTable.Add(UnityContainerType, new BuildPlan { IsCompiled = false });

            this.lifetimeTable.Add(UnityContainerType, new ContainerControlledLifetimeManager());
            this.factoryExpressionTable.Add(UnityContainerType, Expression.Constant(this));

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

            this.lifetimeTable.Add(UnityContainerType, new ContainerControlledLifetimeManager());
            this.factoryExpressionTable.Add(UnityContainerType, Expression.Constant(this));
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
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            if (lifetime == null)
            {
                throw new ArgumentNullException("lifetime");
            }

            if (!string.IsNullOrEmpty(name))
            {
                throw new NotSupportedException("Named registrations are not supported");
            }

            this.RegisteringInstance(this, new RegisterInstanceEventArgs(t, instance, name, lifetime));

            lock (this.lockObj)
            {
                lifetime.SetValue(instance);
                this.lifetimeTable.AddOrUpdate(t, lifetime);
                this.factoryExpressionTable.AddOrUpdate(t, Expression.Constant(instance));

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
            if (to == null)
            {
                throw new ArgumentNullException("to");
            }

            if (injectionMembers == null)
            {
                throw new ArgumentNullException("injectionMembers");
            }

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

            Logger.RegisterType(from ?? to, to, lifetimeManager);
            this.Registering(this, new RegisterEventArgs(from, to, name, lifetimeManager));

            lock (this.lockObj)
            {
                if (from != null)
                {
                    this.typeMappingTable.AddOrUpdate(from, to);
                }

                if (lifetimeManager != null)
                {
                    this.lifetimeTable.AddOrUpdate(to, lifetimeManager);
                }

                if (injectionMembers.Length == 1)
                {
                    this.factoryExpressionTable.AddOrUpdate(to, injectionMembers.Length == 1 ? injectionMembers[0].GenExpression(to, this) : null);
                }

                var plan = new BuildPlan { IsCompiled = false };
                this.buildPlanTable.AddOrUpdate(to, plan);

                if (from != null)
                {
                    this.buildPlanTable.AddOrUpdate(from, plan);
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
            IEnumerable<Type> types = depTree.Sort(node => node.Children).Select(x => x.Value);

            var typeRegistrations = new List<TypeRegistration>();
            foreach (var type in types)
            {
                var mappedType = this.GetMappingFor(type);
                typeRegistrations.Add(new TypeRegistration(type, mappedType, this.GetLifetimeFor(mappedType), this.GetFactoryExpressionFor(mappedType)));
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
            Type mappedType = this.GetMappingFor(type);
            Expression expression = this.GetFactoryExpressionFor(mappedType);

            if (expression != null)
            {
                // container.RegisterType<IFoo>(new InjectionFactory(...))
                if (expression.GetType() == typeof(ParameterizedInjectionFactoryMethodCallExpression))
                {
                    return ((ParameterizedInjectionFactoryMethodCallExpression)expression).DependentTypes;
                }

                // container.RegisterType<IFoo>(new ParameterizedInjectionFactory<IProvider1, IProvider2, IFoo>(...))
                if (expression.GetType() == typeof(ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression))
                {
                    return ((ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression)expression).DependentTypes;
                }

                // Opaque, has no dependencies
                return Enumerable.Empty<Type>();
            }

            // container.RegisterType<IFoo>(new LifetimeManagerWillProvideValue())
            // Special Case: Func<T> that is not registered
            if ((mappedType.GetTypeInfo().IsGenericType && mappedType.GetGenericTypeDefinition() == typeof(Func<>) || mappedType.GetTypeInfo().IsInterface || mappedType.GetTypeInfo().IsAbstract))
            {
                return Enumerable.Empty<Type>();
            }

            // Regular class that can be constructed
            return mappedType.ConstructorDependencies();
        }

        private LifetimeManager GetLifetimeFor(Type type)
        {
            LifetimeManager lifetime;

            if (this.lifetimeTable.TryGetValue(type, out lifetime))
            {
                return lifetime;
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetLifetimeFor(type);
            }

            return new TransientLifetimeManager();
        }

        private Type GetMappingFor(Type type)
        {
            Type mappedType;

            if (this.typeMappingTable.TryGetValue(type, out mappedType))
            {
                return mappedType;
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetMappingFor(type);
            }

            return type;
        }

        private Expression GetFactoryExpressionFor(Type type)
        {
            Expression expression;

            if (this.factoryExpressionTable.TryGetValue(type, out expression))
            {
                return expression;
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetFactoryExpressionFor(type);
            }

            return expression;
        }
    }
}