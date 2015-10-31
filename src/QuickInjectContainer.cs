namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class QuickInjectContainer : IQuickInjectContainer
    {
        private static readonly Type UnityContainerType = typeof(IQuickInjectContainer);
        
        private static readonly QuickInjectEventSource Logger = new QuickInjectEventSource();

        private static readonly ResolutionContextParameterExpression ResolutionContextParameterExpression = new ResolutionContextParameterExpression();

        private readonly object lockObj = new object();

        private readonly QuickInjectContainer parentContainer;

        private readonly Dictionary<Type, Type> typeMappingTable = new Dictionary<Type, Type>();

        private readonly Dictionary<Type, LifetimeManager> lifetimeTable = new Dictionary<Type, LifetimeManager>();

        private readonly Dictionary<Type, Expression> factoryExpressionTable = new Dictionary<Type, Expression>();

        private readonly List<IBuildPlanVisitor> buildPlanVisitors = new List<IBuildPlanVisitor>();

        private readonly ExtensionImpl extensionImpl;

        private readonly List<QuickInjectContainer> children = new List<QuickInjectContainer>();
        
        private ImmutableDictionary<Type, PropertyInfo[]> propertyInfoTable = ImmutableDictionary<Type, PropertyInfo[]>.Empty;

        private ImmutableDictionary<Type, Func<object, object>> buildPlanTable = ImmutableDictionary<Type, Func<object, object>>.Empty;

        private Action<ITreeNode<Type>> dependencyTreeListener;

        private IPropertySelectorPolicy propertySelectorPolicy;

        public QuickInjectContainer()
        {
            this.extensionImpl = new ExtensionImpl(this);

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

            this.factoryExpressionTable.Add(UnityContainerType, Expression.Constant(this));
        }

        internal event EventHandler<RegisterEventArgs> Registering;

        internal event EventHandler<RegisterInstanceEventArgs> RegisteringInstance;

        internal event EventHandler<ChildContainerCreatedEventArgs> ChildContainerCreated;

        public IQuickInjectContainer Parent
        {
            get
            {
                return this.parentContainer;
            }
        }

        public IQuickInjectContainer AddExtension(QuickInjectExtension extension)
        {
            extension.InitializeExtension(this.extensionImpl);
            return this;
        }

        public void SetPropertySelectorPolicy(IPropertySelectorPolicy policy)
        {
            this.propertySelectorPolicy = policy;
        }

        public object BuildUp(Type t, object existing, object resolutionContext)
        {
            PropertyInfo[] propertyInfos = this.propertyInfoTable.GetValueOrDefault(t);
            if (propertyInfos != null)
            {
                foreach (PropertyInfo p in propertyInfos)
                {
                    p.SetValue(existing, this.Resolve(p.PropertyType, resolutionContext));
                }
            }
            else
            {
                this.SlowBuildUp(existing, t, resolutionContext);
            }

            return existing;
        }

        public object BuildUp(Type t, object existing)
        {
#if DEBUG
            if (Logger.IsEnabled())
            {
                Logger.ResolveCallWithoutResolutionContext(t.ToString());
            }
#endif
            
            return this.BuildUp(t, existing, resolutionContext: null);
        }

        public void Dispose()
        {
            
        }
        
        public IQuickInjectContainer CreateChildContainer()
        {
            QuickInjectContainer child;
            ExtensionImpl childContext;

            // The child container collection and build plan visitor collection are enumerated during ClearBuildPlans and child container 
            // instantiation, so we must synchronize to avoid modifying the collections during enumeration.
            lock (this.lockObj)
            {
                child = new QuickInjectContainer(this);
                childContext = new ExtensionImpl(child);
                this.children.Add(child);
            }

            // Must happen outside the lock to avoid deadlock between callers
            this.ChildContainerCreated(this, new ChildContainerCreatedEventArgs(childContext));

            return child;
        }

        public IQuickInjectContainer RegisterInstance(Type t, object instance, LifetimeManager lifetime)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            if (lifetime == null)
            {
                throw new ArgumentNullException("lifetime");
            }

            this.RegisteringInstance(this, new RegisterInstanceEventArgs(t, instance, lifetime));

            lock (this.lockObj)
            {
                lifetime.SetValue(instance);
                this.lifetimeTable.AddOrUpdate(t, lifetime);
                this.factoryExpressionTable.AddOrUpdate(t, Expression.Constant(instance));
                this.ClearBuildPlans();
            }

            return this;
        }

        public IQuickInjectContainer RegisterType(Type from, Type to, LifetimeManager lifetimeManager, InjectionMember injectionMember = null)
        {
            if (to == null)
            {
                throw new ArgumentNullException("to");
            }
            
            if ((from != null && from.GetTypeInfo().IsGenericTypeDefinition) || to.GetTypeInfo().IsGenericTypeDefinition)
            {
                throw new ArgumentException("Open Generic Types are not supported");
            }
            
            this.Registering(this, new RegisterEventArgs(from, to, lifetimeManager));

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

                if (injectionMember != null)
                {
                    this.factoryExpressionTable.AddOrUpdate(to, injectionMember.GenExpression(to, this));
                }

                this.ClearBuildPlans();
            }

            return this;
        }

        public void RegisterResolutionContextType<T>()
        {
            lock (this.lockObj)
            {
                this.factoryExpressionTable.AddOrUpdate(typeof(T), ResolutionContextParameterExpression);
                this.ClearBuildPlans();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type t)
        {
#if DEBUG
            if (Logger.IsEnabled())
            {
                Logger.ResolveCallWithoutResolutionContext(t.ToString());
            }
#endif

            return this.Resolve(t, resolutionContext: null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type t, object resolutionContext)
        {
#if DEBUG
            if (Logger.IsEnabled())
            {
                Logger.Resolve(t.ToString());
            }
#endif

            Func<object, object> plan = this.buildPlanTable.GetValueOrDefault(t);
            return plan != null ? plan(resolutionContext) : this.CompileAndRunPlan(t, resolutionContext);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetService(Type serviceType)
        {
#if DEBUG
            if (Logger.IsEnabled())
            {
                Logger.GetService(serviceType.ToString());
            }
#endif

            Func<object, object> plan = this.buildPlanTable.GetValueOrDefault(serviceType);
            return plan != null ? plan(null) : this.CompileAndRunPlan(serviceType, null);
        }

        public void AddBuildPlanVisitor(IBuildPlanVisitor visitor)
        {
            lock (this.lockObj)
            {
                this.buildPlanVisitors.Add(visitor);
                this.ClearBuildPlans();
            }
        }

        public void RegisterDependencyTreeListener(Action<ITreeNode<Type>> action)
        {
            this.dependencyTreeListener = action;
        }

        /// <summary>
        /// Clears the buildPlanTable for this instance as well as all of its descendants. 
        /// </summary>
        private void ClearBuildPlans()
        {
            this.buildPlanTable = ImmutableDictionary<Type, Func<object, object>>.Empty;
            var childrenStack = new Stack<QuickInjectContainer>();
            childrenStack.Push(this);

            while (childrenStack.Count != 0)
            {
                var curr = childrenStack.Pop();
                curr.buildPlanTable = ImmutableDictionary<Type, Func<object, object>>.Empty;

                foreach (var child in curr.children)
                {
                    childrenStack.Push(child);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private object CompileAndRunPlan(Type t, object resolutionContext)
        {
            string typeString = t.ToString();

            Func<object, object> compiledexpression;

            Expression eptree;

            Logger.CompilationStart(typeString);

            Logger.CompilationContentionStart(typeString);

            lock (this.lockObj)
            {
                Logger.CompilationContentionStop(typeString);

                // Double-check if another thread already compiled a build plan for this type while waiting for the lock. Only compile if there is still no plan.
                compiledexpression = this.buildPlanTable.GetValueOrDefault(t);

                if (compiledexpression == null)
                {
                    Logger.CompilationDependencyAnalysisStart(typeString);

                    var depTree = t.BuildDependencyTree(this.Dependencies, this.TypeRegistrationResolver);

                    Logger.CompilationDependencyAnalysisStop(typeString);

                    Logger.CompilationCodeGenerationStart(typeString);

                    var typeRegistrations = new List<TypeRegistration>();
                    foreach (var item in depTree.Children)
                    {
                        this.AddParameterizedDependants(item.Value, typeRegistrations); // add dependant parameters for each child
                        typeRegistrations.Add(item.Value); // add itself
                    }

                    this.AddParameterizedDependants(depTree.Value, typeRegistrations); // add root dependants
                    typeRegistrations.Add(depTree.Value); // add itself

                    var codeGenerator = new ExpressionGenerator(this, typeRegistrations);
                    eptree = codeGenerator.Generate();

                    eptree = this.buildPlanVisitors.Aggregate(eptree, (current, visitor) => visitor.Visitor(current, t));

                    Logger.CompilationCodeGenerationStop(typeString);

                    Logger.CompilationCodeCompilationStart(typeString);

                    compiledexpression = Expression.Lambda<Func<object, object>>(eptree, "Create_" + t, new[] { codeGenerator.ResolutionContextParameter }).Compile();

                    Logger.CompilationCodeCompilationStop(typeString);

                    this.buildPlanTable = this.buildPlanTable.AddOrUpdate(t, compiledexpression);
                }
            }

            Logger.CompilationStop(typeString);

            return compiledexpression(resolutionContext);
        }

        private TypeRegistration TypeRegistrationResolver(Type type)
        {
            var mappedType = this.GetMappingFor(type);
            return new TypeRegistration(type, mappedType, this.GetLifetimeFor(mappedType), this.GetFactoryExpressionFor(mappedType));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SlowBuildUp(object existing, Type t, object resolutionContext)
        {
            if (this.propertySelectorPolicy != null)
            {
                var selectedProperties = this.propertySelectorPolicy.GetProperties(t).ToArray();
                this.propertyInfoTable = this.propertyInfoTable.AddOrUpdate(t, selectedProperties);

                foreach (var selectedProperty in selectedProperties)
                {
                    selectedProperty.SetValue(existing, this.Resolve(selectedProperty.PropertyType, resolutionContext));
                }
            }
        }

        private IEnumerable<Type> Dependencies(Type type)
        {
            Type mappedType = this.GetMappingFor(type);
            Expression expression = this.GetFactoryExpressionFor(mappedType);

            if (expression != null)
            {
                // container.RegisterType<IFoo>(new ParameterizedInjectionFactory<IProvider1, IProvider2, IFoo>(...))
                if (expression.GetType() == typeof(ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression))
                {
                    return ((ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression)expression).DependentTypes;
                }

                // Opaque, has no dependencies
                return Enumerable.Empty<Type>();
            }

            // container.RegisterType<IFoo>(new LifetimeManagerWillProvideValue())
            if (mappedType.GetTypeInfo().IsInterface || mappedType.GetTypeInfo().IsAbstract)
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

            return null;
        }
        
        private void AddParameterizedDependants(TypeRegistration item, List<TypeRegistration> registrations)
        {
            var factory = item.Factory as ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression;
            if (factory != null)
            {
                foreach (var type in factory.DependentTypes)
                {
                    TypeRegistration typeRegistration = this.TypeRegistrationResolver(type);
                    this.AddParameterizedDependants(typeRegistration, registrations);
                    registrations.Add(typeRegistration);
                }
            }
        }
    }
}