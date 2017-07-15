namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public sealed class QuickInjectContainer : IQuickInjectContainer
    {
        private static readonly QuickInjectEventSource Logger = new QuickInjectEventSource();

        private static readonly Type RequiresRecoveryType = typeof(IRequiresRecovery);

        private static readonly Type ObjectType = typeof(object);

        private static readonly Type ReturnType = ObjectType;

        private static readonly Type IQuickInjectContainerType = typeof(IQuickInjectContainer);

        private static readonly Type QuickInjectContainerType = typeof(QuickInjectContainer);

        private static readonly Type LifetimeManagerArrayType = typeof(LifetimeManager[]);

        private static readonly Type ObjectArrayType = typeof(object[]);

        private static readonly Type Int32Type = typeof(int);

        private static readonly Type[] ParameterTypes = { QuickInjectContainerType, LifetimeManagerArrayType, ObjectArrayType, ObjectType, Int32Type };

        private static readonly Type LifetimeManagerType = typeof(LifetimeManager);

        private static readonly Type ExceptionType = typeof(Exception);

        private static readonly Type ImmutableArrayType = typeof(ImmutableArray<IntPtr>);

        private static readonly MethodInfo CompileMethodInfo = typeof(QuickInjectContainer).GetMethod("Compile", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo JumpTableFieldInfo = QuickInjectContainerType.GetField("jumpTable", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo ImmutableArrayGetItemMethodInfo = ImmutableArrayType.GetMethod("get_Item");

        private static readonly MethodInfo GetValueMethodInfo = LifetimeManagerType.GetMethod("GetValue", new Type[] { ObjectType });

        private static readonly MethodInfo SetValueMethodInfo = LifetimeManagerType.GetMethod("SetValue", new Type[] { ObjectType, ObjectType });

        private static readonly MethodInfo ResolveInternalCall = QuickInjectContainerType.GetMethod("ResolveInternal");

        private static readonly MethodInfo NonConstructableTypeMethodInfo = QuickInjectContainerType.GetMethod("ThrowNonConstructableType");

        private static readonly MethodInfo RethrowExceptionMethodInfo = QuickInjectContainerType.GetMethod("RethrowException");

        private static readonly MethodInfo GetMethodDescriptorMethodInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Dictionary<Type, int> EmptyDict = new Dictionary<Type, int>();

        private static readonly object StaticLock = new object();

        private static GrowableArray<LifetimeManager> LifetimeManagers = new GrowableArray<LifetimeManager>(1000);

        private static GrowableArray<object> Constants = new GrowableArray<object>(1000);

        private readonly ICompilationMonitor compilationMonitor;

        private readonly QuickInjectContainer parentContainer;

        private readonly List<QuickInjectContainer> children = new List<QuickInjectContainer>();

        private readonly Dictionary<Type, Type> typeMappingTable = new Dictionary<Type, Type>();

        private readonly Dictionary<Type, LifetimeManager> lifetimeTable = new Dictionary<Type, LifetimeManager>();

        private readonly Dictionary<LifetimeManager, int> lifetimeIndexTable = new Dictionary<LifetimeManager, int>();

        private readonly Dictionary<Type, int> constantIndexTable = new Dictionary<Type, int>();

        private readonly Dictionary<Type, Expression> factoryExpressionTable = new Dictionary<Type, Expression>();

        private readonly object compileLock = new object();

        private Dictionary<Type, PropertyInfo[]> propertyInfoTable = new Dictionary<Type, PropertyInfo[]>();

        private ImmutableArray<IntPtr> jumpTable = ImmutableArray<IntPtr>.Empty;

        private ImmutableList<DynamicMethod> dynamicMethods = ImmutableList<DynamicMethod>.Empty;

        private IPropertySelectorPolicy propertySelectorPolicy;

        private ExtensionImpl extensionImpl;

        private PerfectHashProvider perfectHashProvider;

        public QuickInjectContainer()
        {
            RuntimeHelpers.PrepareMethod(CompileMethodInfo.MethodHandle);
            this.InitializeContainer();
        }

        public QuickInjectContainer(ICompilationMonitor compilationMonitor)
            : this()
        {
            this.compilationMonitor = compilationMonitor;
        }

        private QuickInjectContainer(QuickInjectContainer parent)
        {
            this.Registering += (sender, args) => { };
            this.RegisteringInstance += (sender, args) => { };
            this.ChildContainerCreated += (sender, args) => { };

            this.parentContainer = parent ?? throw new ArgumentNullException(nameof(parent));
            this.extensionImpl = this.parentContainer.extensionImpl;

            this.perfectHashProvider = this.parentContainer.perfectHashProvider;
            this.perfectHashProvider.AddContainer(this);

            this.factoryExpressionTable.Add(IQuickInjectContainerType, Expression.Constant(this));
        }

        internal event EventHandler<RegisterEventArgs> Registering;

        internal event EventHandler<RegisterInstanceEventArgs> RegisteringInstance;

        internal event EventHandler<ChildContainerCreatedEventArgs> ChildContainerCreated;

        public IQuickInjectContainer Parent => this.parentContainer;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RethrowException(Exception e)
        {
            throw e;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNonConstructableType(IntPtr typeHandle)
        {
            throw new Exception("Cannot construct type: " + GetTypeFromHandle(typeHandle));
        }

        public IQuickInjectContainer RegisterType(Type from, Type to, LifetimeManager lifetimeManager, InjectionMember injectionMember = null)
        {
            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            if ((from != null && from.GetTypeInfo().IsGenericTypeDefinition) || to.GetTypeInfo().IsGenericTypeDefinition)
            {
                throw new ArgumentException("Open Generic Types are not supported");
            }

            var handler = this.Registering;
            handler?.Invoke(this, new RegisterEventArgs(from, to, lifetimeManager));

            lock (this.compileLock)
            {
                if (from != null)
                {
                    this.typeMappingTable.AddOrUpdate(from, to);
                }

                if (lifetimeManager != null)
                {
                    this.lifetimeTable.AddOrUpdate(to, lifetimeManager);

                    int index;
                    lock (StaticLock)
                    {
                        LifetimeManagers.Add(lifetimeManager);
                        index = LifetimeManagers.Count - 1;
                    }

                    this.lifetimeIndexTable.Add(lifetimeManager, index);
                }

                if (injectionMember != null)
                {
                    this.factoryExpressionTable.AddOrUpdate(to, injectionMember.GenExpression(to, this));
                }

                this.ClearBuildPlans();
            }

            return this;
        }

        public IQuickInjectContainer RegisterInstance(Type t, object instance, LifetimeManager lifetimeManager)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (lifetimeManager == null)
            {
                throw new ArgumentNullException(nameof(lifetimeManager));
            }

            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            var handler = this.RegisteringInstance;
            handler?.Invoke(this, new RegisterInstanceEventArgs(t, instance, lifetimeManager));
            lifetimeManager.SetValue(null, instance);

            lock (this.compileLock)
            {
                this.lifetimeTable.AddOrUpdate(t, lifetimeManager);

                int index, index2;
                lock (StaticLock)
                {
                    LifetimeManagers.Add(lifetimeManager);
                    index = LifetimeManagers.Count - 1;
                    Constants.Add(instance);
                    index2 = Constants.Count - 1;
                }

                this.lifetimeIndexTable.Add(lifetimeManager, index);
                this.constantIndexTable.Add(t, index2);

                this.factoryExpressionTable.AddOrUpdate(t, Expression.Constant(instance));
                this.ClearBuildPlans();
            }

            return this;
        }

        public IQuickInjectContainer RegisterTypeAsResolutionContext<T>()
        {
            Type type = typeof(T);
            lock (this.compileLock)
            {
                this.factoryExpressionTable.AddOrUpdate(type, new ResolveResolutionContextExpression());
                this.lifetimeTable.Remove(type);
                this.typeMappingTable.Remove(type);
                this.ClearBuildPlans();
            }

            return this;
        }

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

        public object Resolve(Type t, object resolutionContext)
        {
#if DEBUG
            if (Logger.IsEnabled())
            {
                Logger.Resolve(t.ToString());
            }
#endif
            var index = this.perfectHashProvider.GetUniqueId(t);
            return CallIndirect(this, LifetimeManagers.UnderlyingArray, Constants.UnderlyingArray, resolutionContext, index, this.jumpTable[index]);
        }

        public object ResolveInternal(LifetimeManager[] lifetimeManagers, object[] constants, object resolutionContext, int typeIndex)
        {
            return CallIndirect(this, lifetimeManagers, constants, resolutionContext, typeIndex, this.jumpTable[typeIndex]);
        }

        public object GetService(Type serviceType)
        {
#if DEBUG
            if (Logger.IsEnabled())
            {
                Logger.GetService(serviceType.ToString());
            }
#endif
            return this.Resolve(serviceType, null);
        }

        public IQuickInjectContainer CreateChildContainer()
        {
            QuickInjectContainer child;
            ExtensionImpl childContext;

            // The child container collection and build plan visitor collection are enumerated during ClearBuildPlans and child container
            // instantiation, so we must synchronize to avoid modifying the collections during enumeration.
            lock (this.compileLock)
            {
                child = new QuickInjectContainer(this);
                childContext = new ExtensionImpl(child);
                this.children.Add(child);
            }

            // Must happen outside the lock to avoid deadlock between callers
            var handler = this.ChildContainerCreated;
            handler?.Invoke(this, new ChildContainerCreatedEventArgs(childContext));

            return child;
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
            if (this.propertyInfoTable.TryGetValue(t, out PropertyInfo[] propertyInfos))
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

        private static Type GetTypeFromHandle(IntPtr handle)
        {
            var method = typeof(Type).GetMethod("GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.NonPublic);
            return (Type)method.Invoke(null, new object[] { handle });
        }

        private static void EmitProlog(
            ILGenerator ilGenerator,
            int lifetimeManagerIndex,
            bool emitExceptionHandling,
            MethodInfo getValueMethodInfo,
            Type lifetimeManagerType,
            Type objectType,
            ref Label label)
        {
            ilGenerator.DeclareLocal(lifetimeManagerType);
            ilGenerator.DeclareLocal(objectType);

            // main prolog
            {
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldc_I4, lifetimeManagerIndex);
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ldarg_3);
                ilGenerator.Emit(OpCodes.Callvirt, getValueMethodInfo);
                ilGenerator.Emit(OpCodes.Stloc_1);
                ilGenerator.Emit(OpCodes.Ldloc_1);
                ilGenerator.Emit(OpCodes.Brtrue, label);
            }

            if (emitExceptionHandling)
            {
                ilGenerator.BeginExceptionBlock();
            }
        }

        private static void EmitEpilog(
            ILGenerator ilGenerator,
            bool emitExceptionHandling,
            MethodInfo setValueMethodInfo,
            MethodInfo recoverMethodInfo,
            MethodInfo rethrowMethodInfo,
            Type exceptionType,
            ref Label label)
        {
            // main epilog
            {
                ilGenerator.Emit(OpCodes.Stloc_1);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ldarg_3);
                ilGenerator.Emit(OpCodes.Ldloc_1);
                ilGenerator.Emit(OpCodes.Callvirt, setValueMethodInfo);
            }

            if (emitExceptionHandling)
            {
                ilGenerator.BeginCatchBlock(exceptionType);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Callvirt, recoverMethodInfo);
                ilGenerator.Emit(OpCodes.Call, rethrowMethodInfo);
                ilGenerator.EndExceptionBlock();
            }

            // label target for successful lifetime check
            {
                ilGenerator.MarkLabel(label);
            }

            ilGenerator.Emit(OpCodes.Ldloc_1);

            ilGenerator.Emit(OpCodes.Ret);
        }

        private static IEnumerable<Type> ConstructorDependencies(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var ctors = typeInfo.DeclaredConstructors.Where(t => t.IsPublic);
            if (typeInfo.IsInterface || typeInfo.IsAbstract || !ctors.Any())
            {
                throw new Exception("Don't know to how to make " + type);
            }

            var parameters = GetLongestConstructor(type).GetParameters();

            // parameterless
            if (parameters.Length == 0)
            {
                return Enumerable.Empty<Type>();
            }

            return parameters.Select(t => t.ParameterType);
        }

        private static ConstructorInfo GetLongestConstructor(Type type)
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

        [CompilerIntrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static extern object CallIndirect(QuickInjectContainer container, LifetimeManager[] lifetimeManagers, object[] constants, object resolutionContext, int index, IntPtr nativeFunctionPointer);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private object Compile(LifetimeManager[] lifetimeManagers, object[] constants, object resolutionContext, int typeIndex)
        {
            // Lifetime managers is empty at QuickInject start (across the entire program, not just the instance)
            lock (StaticLock)
            {
                if (LifetimeManagers.Empty)
                {
                    LifetimeManagers.Add(TransientLifetimeManager.Default);
                }
            }

            var t = this.perfectHashProvider.GetTypeFromIndex(typeIndex);
            string typeString = t.ToString();

            this.compilationMonitor?.Begin(t, resolutionContext);
            Logger.CompilationStart(typeString);
            Logger.CompilationContentionStart(typeString);

            lock (this.compileLock)
            {
                Logger.CompilationContentionStop(typeString);

                IntPtr methodPtr = this.jumpTable[typeIndex];

                // Double-check if another thread already compiled a build plan for this type while waiting for the lock. Only compile if there is still no plan.
                if (methodPtr != CompileMethodInfo.MethodHandle.GetFunctionPointer())
                {
                    return CallIndirect(this, lifetimeManagers, constants, resolutionContext, -1, methodPtr);
                }

                Logger.CompilationCodeGenerationStart(typeString);

                var lifetime = this.GetLifetimeFor(this.GetMappingFor(t));
                var emitExceptionHandling = lifetime.GetType().GetTypeInfo().ImplementedInterfaces.Any(x => x == RequiresRecoveryType);

                var dynamicMethod = new DynamicMethod("Create_" + t, ReturnType, ParameterTypes, QuickInjectContainerType.Module, skipVisibility: true);
                var ilGenerator = dynamicMethod.GetILGenerator();

                var label = ilGenerator.DefineLabel();

                EmitProlog(ilGenerator, this.GetLifetimeIndexFor(lifetime), emitExceptionHandling, GetValueMethodInfo, lifetime.GetType(), ObjectType, ref label);

                Logger.CompilationDependencyAnalysisStart(typeString);

                var immediateDependencies = this.ImmediateDependencies(t, out ConstructorInfo ctor).ToArray();

                Logger.CompilationDependencyAnalysisStop(typeString);

                if (ctor != null)
                {
                    var extendedPrologResolveCallsMap = this.CreateExtendedPrologResolveCallsMap(immediateDependencies);

                    foreach (var pair in extendedPrologResolveCallsMap)
                    {
                        var type = pair.Key;
                        var index = pair.Value;

                        ilGenerator.DeclareLocal(type);

                        this.WriteResolveInternalCall(ilGenerator, t);

                        ilGenerator.Emit(OpCodes.Stloc, index);
                    }

                    foreach (var immediateDependency in immediateDependencies)
                    {
                        this.CodeGen(ilGenerator, immediateDependency, extendedPrologResolveCallsMap, topLevelCodeGen: false);
                    }

                    ilGenerator.Emit(OpCodes.Newobj, ctor);
                }
                else
                {
                    this.CodeGen(ilGenerator, t, EmptyDict, topLevelCodeGen: true);
                }

                EmitEpilog(ilGenerator, emitExceptionHandling, SetValueMethodInfo, lifetime.GetType().GetMethod("Recover"), RethrowExceptionMethodInfo, ExceptionType, ref label);

                Logger.CompilationCodeGenerationStop(typeString);

                Logger.CompilationCodeCompilationStart(typeString);

                var runtimeMethodHandle = (RuntimeMethodHandle)GetMethodDescriptorMethodInfo.Invoke(dynamicMethod, null);
                RuntimeHelpers.PrepareMethod(runtimeMethodHandle); // JIT
                methodPtr = runtimeMethodHandle.GetFunctionPointer();

                Logger.CompilationCodeCompilationStop(typeString);

                Logger.CompilationDataStructureCopyStart(typeString);

                this.jumpTable = this.jumpTable.SetItem(typeIndex, methodPtr);
                this.dynamicMethods = this.dynamicMethods.Add(dynamicMethod);

                Logger.CompilationDataStructureCopyStop(typeString);

                this.compilationMonitor?.End(t, resolutionContext);
                Logger.CompilationStop(typeString);

                return CallIndirect(this, LifetimeManagers.UnderlyingArray, Constants.UnderlyingArray, resolutionContext, -1, methodPtr);
            }
        }

        private void CodeGen(ILGenerator ilGenerator, Type type, Dictionary<Type, int> dict, bool topLevelCodeGen)
        {
            var mappedType = this.GetMappingFor(type);
            var factoryExpression = this.GetFactoryExpressionFor(mappedType);

            if (factoryExpression != null)
            {
                if (factoryExpression is ConstantExpression)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    ilGenerator.Emit(OpCodes.Ldc_I4, this.GetConstantIndexFor(mappedType));
                    ilGenerator.Emit(OpCodes.Ldelem_Ref);
                }
                else if (factoryExpression is ResolveResolutionContextExpression)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_3);
                }
                else
                {
                    var parameterized = factoryExpression as ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression;
                    if (parameterized != null)
                    {
                        parameterized.GenerateCode(ilGenerator, dict);
                    }
                    else
                    {
                        throw new NotSupportedException("Unknown expression type passed");
                    }
                }
            }
            else
            {
                if (topLevelCodeGen)
                {
                    ilGenerator.Emit(OpCodes.Call, NonConstructableTypeMethodInfo); // returns object
                }
                else
                {
                    this.WriteResolveInternalCall(ilGenerator, type);
                }
            }
        }

        private void WriteResolveInternalCall(ILGenerator ilGenerator, Type t)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Ldarg_3);
            ilGenerator.Emit(OpCodes.Ldc_I4, this.perfectHashProvider.GetUniqueId(t));
            ilGenerator.Emit(OpCodes.Call, ResolveInternalCall);
        }

        private IEnumerable<Type> ImmediateDependencies(Type type, out ConstructorInfo ctor)
        {
            Type mappedType = this.GetMappingFor(type);
            Expression expression = this.GetFactoryExpressionFor(mappedType);
            ctor = null;

            // Opaque, has no dependencies
            if (expression != null)
            {
                return Enumerable.Empty<Type>();
            }

            // container.RegisterType<IFoo>(new LifetimeManagerWillProvideValue())
            var typeInfo = mappedType.GetTypeInfo();
            if (typeInfo.IsInterface || typeInfo.IsAbstract)
            {
                return Enumerable.Empty<Type>();
            }

            // Regular class that can be constructed
            ctor = GetLongestConstructor(mappedType);
            return ConstructorDependencies(mappedType);
        }

        private Dictionary<Type, int> CreateExtendedPrologResolveCallsMap(IEnumerable<Type> types)
        {
            var typeDict = new Dictionary<Type, int>();
            int index = 2; // 0 and 1 are taken by container and lifetimemanagers[].

            foreach (var type in types)
            {
                var mappedType = this.GetMappingFor(type);
                var expression = this.GetFactoryExpressionFor(mappedType);

                var parameterized = expression as ParameterizedLambdaExpressionInjectionFactoryMethodCallExpression;

                if (parameterized != null)
                {
                    foreach (var dependentType in parameterized.DependentTypes)
                    {
                        if (!typeDict.ContainsKey(dependentType))
                        {
                            typeDict.Add(type, index);
                            index++;
                        }
                    }
                }
            }

            return typeDict;
        }

        private void InitializeContainer()
        {
            this.extensionImpl = new ExtensionImpl(this);
            this.perfectHashProvider = new PerfectHashProvider(CompileMethodInfo.MethodHandle.GetFunctionPointer()); // this instance is used by all containers of a heirarchy
            this.perfectHashProvider.AddContainer(this);
            this.factoryExpressionTable.Add(IQuickInjectContainerType, Expression.Constant(this));
            this.lifetimeIndexTable.Add(TransientLifetimeManager.Default, 0);

            this.Registering += (sender, args) => { };
            this.RegisteringInstance += (sender, args) => { };
            this.ChildContainerCreated += (sender, args) => { };
        }

        private void ClearBuildPlans()
        {
            this.jumpTable = ImmutableArray<IntPtr>.Empty;
            this.dynamicMethods = ImmutableList<DynamicMethod>.Empty;

            var childrenStack = new Stack<QuickInjectContainer>();
            childrenStack.Push(this);

            while (childrenStack.Count != 0)
            {
                var curr = childrenStack.Pop();

                curr.jumpTable = ImmutableArray<IntPtr>.Empty;
                curr.dynamicMethods = ImmutableList<DynamicMethod>.Empty;

                foreach (var child in curr.children)
                {
                    childrenStack.Push(child);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SlowBuildUp(object existing, Type t, object resolutionContext)
        {
            if (this.propertySelectorPolicy != null)
            {
                lock (this.propertySelectorPolicy)
                {
                    var selectedProperties = this.propertySelectorPolicy.GetProperties(t).ToArray();

                    var newPropertyTable = new Dictionary<Type, PropertyInfo[]>();

                    foreach (var elem in this.propertyInfoTable)
                    {
                        newPropertyTable.Add(elem.Key, elem.Value);
                    }

                    newPropertyTable.AddOrUpdate(t, selectedProperties);

                    // atomic swap
                    this.propertyInfoTable = newPropertyTable;

                    foreach (var selectedProperty in selectedProperties)
                    {
                        selectedProperty.SetValue(existing, this.Resolve(selectedProperty.PropertyType, resolutionContext));
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private LifetimeManager GetLifetimeFor(Type type)
        {
            if (this.lifetimeTable.TryGetValue(type, out LifetimeManager lifetime))
            {
                return lifetime;
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetLifetimeFor(type);
            }

            return TransientLifetimeManager.Default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int GetConstantIndexFor(Type type)
        {
            if (this.constantIndexTable.TryGetValue(type, out int index))
            {
                return index;
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetConstantIndexFor(type);
            }

            throw new Exception($"Constant of type {type} was not found.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int GetLifetimeIndexFor(LifetimeManager lifetimeManager)
        {
            if (this.lifetimeIndexTable.TryGetValue(lifetimeManager, out int index))
            {
                return index;
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetLifetimeIndexFor(lifetimeManager);
            }

            throw new Exception($"Lifetime of type {lifetimeManager.GetType()} was not found.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Type GetMappingFor(Type type)
        {
            if (this.typeMappingTable.TryGetValue(type, out Type mappedType))
            {
                return mappedType;
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetMappingFor(type);
            }

            return type;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Expression GetFactoryExpressionFor(Type type)
        {
            if (this.factoryExpressionTable.TryGetValue(type, out Expression expression))
            {
                return expression;
            }

            while (this.parentContainer != null)
            {
                return this.parentContainer.GetFactoryExpressionFor(type);
            }

            return null;
        }

        private sealed class PerfectHashProvider
        {
            private readonly List<Type> types = new List<Type>();

            private readonly object lockObj = new object();

            private readonly List<QuickInjectContainer> containers = new List<QuickInjectContainer>();

            private readonly IntPtr compileMethodPtr;

            private ImmutableDictionary<Type, int> perfectHashMap;

            public PerfectHashProvider(IntPtr compileMethodPtr)
            {
                this.perfectHashMap = ImmutableDictionary<Type, int>.Empty;
                this.compileMethodPtr = compileMethodPtr;
            }

            public Type GetTypeFromIndex(int index)
            {
                lock (this.lockObj)
                {
                    // yes, linear search. I just want this working for now.
                    for (int i = 0; i < this.types.Count; ++i)
                    {
                        if (i == index)
                        {
                            return this.types[i];
                        }
                    }
                }

                throw new Exception($"No type found at index {index}");
            }

            public void AddContainer(QuickInjectContainer container)
            {
                lock (this.lockObj)
                {
                    var tmp = ImmutableArray<IntPtr>.Empty;

                    for (int i = 0; i < this.types.Count; ++i)
                    {
                        tmp = tmp.Add(this.compileMethodPtr);
                    }

                    this.containers.Add(container);

                    // jump tables need to be setup after adding the container
                    Thread.MemoryBarrier();

                    container.jumpTable = tmp;
                }
            }

            public int GetUniqueId(Type t)
            {
                if (!this.perfectHashMap.TryGetValue(t, out int value))
                {
                    this.SlowLookup(t, out value);
                }

                return value;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowLookup(Type t, out int value)
            {
                lock (this.lockObj)
                {
                    if (!this.perfectHashMap.TryGetValue(t, out value))
                    {
                        this.types.Add(t);
                        value = this.types.Count - 1;

                        foreach (var container in this.containers)
                        {
                            container.jumpTable = container.jumpTable.Add(this.compileMethodPtr);
                        }

                        // prevent this.perfectHashMap is assigned after the jump tables are setup
                        // gotta think about ARM :)
                        Thread.MemoryBarrier();

                        this.perfectHashMap = this.perfectHashMap.Add(t, value);
                    }
                }
            }
        }
    }
}