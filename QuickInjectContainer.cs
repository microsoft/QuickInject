// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;

    public sealed class QuickInjectContainer : IQuickInjectContainer
    {
        private const int InvalidLifetimeIndex = -1;

        private const int InvalidLocalIndex = -1;

        private static readonly Type ObjectType = typeof(object);

        private static readonly Type IQuickInjectContainerType = typeof(IQuickInjectContainer);

        private static readonly Type QuickInjectContainerType = typeof(QuickInjectContainer);

        private static readonly Type LifetimeManagerType = typeof(LifetimeManager);

        private static readonly Type TransientLifetimeManagerType = typeof(TransientLifetimeManager);

        private static readonly Type[] ParameterTypes = { ObjectType, typeof(LifetimeManager[]) };

        private static readonly Type[] EmptyTypeArray = { };

        private static readonly MethodInfo ThrowUnconstructableTypeMethodInfo = QuickInjectContainerType.GetRuntimeMethods().Single(x => x.Name == "ThrowUnconstructableType");

        private static readonly MethodInfo GetValueMethod = LifetimeManagerType.GetRuntimeMethods().Single(x => x.Name == "GetValue" && x.GetParameters().Length == 1);

        private static readonly MethodInfo SetValueMethod = LifetimeManagerType.GetRuntimeMethods().Single(x => x.Name == "SetValue" && x.GetParameters().Length == 2);

        private static readonly MethodInfo GetMethodDescriptorMethodInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly TransientLifetimeManager TransientLifetimeManagerInstance = new TransientLifetimeManager();

        private static readonly InstanceInjectionMember InstanceInjectionMemberInstance = new InstanceInjectionMember();

        private readonly List<QuickInjectContainer> children = new List<QuickInjectContainer>();

        private readonly Dictionary<Type, Type> typeMappingTable = new Dictionary<Type, Type>();

        private readonly Dictionary<Type, int> lifetimeTable = new Dictionary<Type, int>();

        private readonly Dictionary<Type, MethodInfo> factoryTable = new Dictionary<Type, MethodInfo>();

        private readonly LifetimeManager[] lifetimeManagers = new LifetimeManager[1024];

        private readonly object lockObj = new object();

        private readonly QuickInjectContainer parentContainer;

        private Type resolutionContextType;

        private int lifetimeIndex;

        private ImmutableDictionary<Type, Tuple<DynamicMethod, IntPtr>> dynamicMethodsDictionary = ImmutableDictionary<Type, Tuple<DynamicMethod, IntPtr>>.Empty;

        private ImmutableDictionary<Type, PropertyInfo[]> propertyInfoTable = ImmutableDictionary<Type, PropertyInfo[]>.Empty;

        private IPropertySelectorPolicy propertySelectorPolicy;

        private ExtensionImpl extensionImpl;

        public QuickInjectContainer()
        {
            this.InitializeContainer();
        }

        private QuickInjectContainer(QuickInjectContainer parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            this.Registering += (sender, args) => { };
            this.RegisteringInstance += (sender, args) => { };
            this.ChildContainerCreated += (sender, args) => { };

            this.parentContainer = parent;
            this.extensionImpl = this.parentContainer.extensionImpl;

            var lifetimeManager = new ContainerControlledLifetimeManager();
            lifetimeManager.SetValue(this);
            this.RegisterType(IQuickInjectContainerType, QuickInjectContainerType, lifetimeManager);
        }

        internal event EventHandler<RegisterEventArgs> Registering;

        internal event EventHandler<RegisterInstanceEventArgs> RegisteringInstance;

        internal event EventHandler<ChildContainerCreatedEventArgs> ChildContainerCreated;

        public IQuickInjectContainer Parent => this.parentContainer;

        public static object ThrowUnconstructableType(int typeIndex)
        {
            throw new Exception("Type Index: " + typeIndex + " is not constructible");
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
            var handler = this.ChildContainerCreated;
            handler?.Invoke(this, new ChildContainerCreatedEventArgs(childContext));

            return child;
        }

        public IQuickInjectContainer RegisterTypeAsResolutionContext<T>()
        {
            var type = typeof(T);
            lock (this.lockObj)
            {
                this.resolutionContextType = type;
                this.lifetimeTable.Remove(type);
                this.typeMappingTable.Remove(type);
                this.ClearBuildPlans();
            }

            return this;
        }

        public IQuickInjectContainer RegisterInstance(Type t, object instance, LifetimeManager lifetime)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (lifetime == null)
            {
                throw new ArgumentNullException(nameof(lifetime));
            }

            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            var handler = this.RegisteringInstance;
            handler?.Invoke(this, new RegisterInstanceEventArgs(t, instance, lifetime));

            lifetime.SetValue(null, instance);
            this.RegisterType(null, t, lifetime, InstanceInjectionMemberInstance);

            return this;
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

            lock (this.lockObj)
            {
                if (from != null)
                {
                    this.typeMappingTable.AddOrUpdate(from, to);
                }

                if (lifetimeManager != null)
                {
                    int index = this.lifetimeIndex++;

                    this.lifetimeTable.AddOrUpdate(to, index);
                    this.lifetimeManagers[index] = lifetimeManager;
                }

                if (injectionMember != null)
                {
                    this.factoryTable.AddOrUpdate(to, injectionMember.Factory);
                }

                this.ClearBuildPlans();
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>(object resolutionContext)
        {
            return (T)this.Resolve(typeof(T), resolutionContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type t)
        {
            return this.Resolve(t, resolutionContext: null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>()
        {
            return (T)this.Resolve(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetService(Type t)
        {
            return this.Resolve(t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type type, object resolutionContext)
        {
            Tuple<DynamicMethod, IntPtr> value;
            if (this.dynamicMethodsDictionary.TryGetValue(type, out value))
            {
                return QuickInjectHelper.CallIndirect(resolutionContext, this.lifetimeManagers, value.Item2);
            }

            return QuickInjectHelper.CallIndirect(resolutionContext, this.lifetimeManagers, this.Compile(type));
        }

        private static void EmitCallIndirect(QuickInjectContainer container, ILGenerator ilGenerator, Type type)
        {
            Tuple<DynamicMethod, IntPtr> tuple;
            IntPtr address = !container.dynamicMethodsDictionary.TryGetValue(type, out tuple) ? container.Compile(type) : tuple.Item2;

            ilGenerator.Emit(OpCodes.Ldarg_0); // object resolutionContext
            ilGenerator.Emit(OpCodes.Ldarg_1); // LifetimeManager[] array
            EmitAddress(ilGenerator, address);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, ObjectType, ParameterTypes, EmptyTypeArray);
        }

        private static void CompileConstructorBasedType(ILGenerator ilGenerator, int lifetimeManagerIndex, ConstructorInfo ctorInfo, IEnumerable<Type> types, bool skipGetValueSetValueCalls, QuickInjectContainer container, int depth, Dictionary<Type, int> localIndexTable)
        {
            var singleReturnLabel = ilGenerator.DefineLabel();
            int localIndex = InvalidLocalIndex;

            if (!skipGetValueSetValueCalls)
            {
                localIndex = ilGenerator.DeclareLocal(ObjectType).LocalIndex;
                EmitGetValueCallWithEarlyReturn(ilGenerator, lifetimeManagerIndex, singleReturnLabel, localIndex);
            }

            foreach (var type in types)
            {
                int cachedLocal;
                if (type == container.resolutionContextType)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                }
                else if (localIndexTable.TryGetValue(type, out cachedLocal) && cachedLocal != InvalidLocalIndex)
                {
                    ilGenerator.Emit(OpCodes.Ldloc, cachedLocal);
                }
                else if (container.ShouldInline(type, depth))
                {
                    int newDepth = depth + 1;
                    container.CompileInternal(type, ilGenerator, localIndexTable, newDepth);
                }
                else
                {
                    EmitCallIndirect(container, ilGenerator, type);
                }
            }

            ilGenerator.Emit(OpCodes.Newobj, ctorInfo);

            if (!skipGetValueSetValueCalls)
            {
                ilGenerator.Emit(OpCodes.Stloc, localIndex);
                EmitSetValueCall(ilGenerator, lifetimeManagerIndex, localIndex);
                ilGenerator.MarkLabel(singleReturnLabel);
                ilGenerator.Emit(OpCodes.Ldloc, localIndex);
            }
        }

        private static void CompileFactoryType(ILGenerator ilGenerator, int lifetimeManagerIndex, MethodInfo factoryMethod, bool skipGetValueSetValueCalls, Dictionary<Type, int> localIndexTable)
        {
            var singleReturnLabel = ilGenerator.DefineLabel();
            int localIndex = InvalidLocalIndex;

            if (!skipGetValueSetValueCalls)
            {
                localIndex = ilGenerator.DeclareLocal(ObjectType).LocalIndex;
                EmitGetValueCallWithEarlyReturn(ilGenerator, lifetimeManagerIndex, singleReturnLabel, localIndex);
            }

            var parameters = factoryMethod.GetParameters().Select(t => t.ParameterType);

            foreach (var parameter in parameters)
            {
                ilGenerator.Emit(OpCodes.Ldloc, localIndexTable[parameter]);
            }

            ilGenerator.Emit(OpCodes.Call, factoryMethod);

            if (!skipGetValueSetValueCalls)
            {
                ilGenerator.Emit(OpCodes.Stloc, localIndex);
                EmitSetValueCall(ilGenerator, lifetimeManagerIndex, localIndex);
                ilGenerator.MarkLabel(singleReturnLabel);
                ilGenerator.Emit(OpCodes.Ldloc, localIndex);
            }
        }

        private static void CompileUnconstructibleType(ILGenerator ilGenerator, int lifetimeManagerIndex)
        {
            var singleReturnLabel = ilGenerator.DefineLabel();
            int localIndex = ilGenerator.DeclareLocal(ObjectType).LocalIndex;

            EmitGetValueCallWithEarlyReturn(ilGenerator, lifetimeManagerIndex, singleReturnLabel, localIndex);
            ilGenerator.Emit(OpCodes.Ldc_I4, lifetimeManagerIndex);
            ilGenerator.Emit(OpCodes.Call, ThrowUnconstructableTypeMethodInfo);

            ilGenerator.MarkLabel(singleReturnLabel);
            ilGenerator.Emit(OpCodes.Ldloc, localIndex);
        }

        private static void CompileInstanceType(ILGenerator ilGenerator, int lifetimeManagerIndex)
        {
            EmitGetValueCall(ilGenerator, lifetimeManagerIndex);
        }

        private static void EmitGetValueCall(ILGenerator ilGenerator, int lifetimeManagerIndex)
        {
            // TransientLifetimeManager doesn't need a GetValue call
            ilGenerator.Emit(OpCodes.Ldarg_1); // LifetimeManager[] array
            ilGenerator.Emit(OpCodes.Ldc_I4, lifetimeManagerIndex);
            ilGenerator.Emit(OpCodes.Ldelem_Ref);
            ilGenerator.Emit(OpCodes.Ldarg_0); // object resolutionContext
            ilGenerator.Emit(OpCodes.Call, GetValueMethod); // object resolutionContext
        }

        private static void EmitGetValueCallWithEarlyReturn(ILGenerator ilGenerator, int lifetimeManagerIndex, Label returnLabel, int localIndex)
        {
            EmitGetValueCall(ilGenerator, lifetimeManagerIndex);
            ilGenerator.Emit(OpCodes.Stloc, localIndex);
            ilGenerator.Emit(OpCodes.Ldloc, localIndex);
            ilGenerator.Emit(OpCodes.Brfalse_S, returnLabel);
        }

        private static void EmitSetValueCall(ILGenerator ilGenerator, int lifetimeManagerIndex, int localIndex)
        {
            ilGenerator.Emit(OpCodes.Ldarg_1); // LifetimeManager[] array
            ilGenerator.Emit(OpCodes.Ldc_I4, lifetimeManagerIndex);
            ilGenerator.Emit(OpCodes.Ldelem_Ref);
            ilGenerator.Emit(OpCodes.Ldarg_0); // object resolutionContext
            ilGenerator.Emit(OpCodes.Ldloc, localIndex);
            ilGenerator.Emit(OpCodes.Call, SetValueMethod); // object resolutionContext
        }

        private static void EmitAddress(ILGenerator ilGenerator, IntPtr address)
        {
            if (IntPtr.Size == 8)
            {
                ilGenerator.Emit(OpCodes.Ldc_I8, address.ToInt64());
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4, address.ToInt32());
            }
        }

        private void InitializeContainer()
        {
            this.extensionImpl = new ExtensionImpl(this);

            var lifetimeManager = new ContainerControlledLifetimeManager();
            lifetimeManager.SetValue(this);
            this.RegisterType(IQuickInjectContainerType, QuickInjectContainerType, lifetimeManager);

            this.Registering += (sender, args) => { };
            this.RegisteringInstance += (sender, args) => { };
            this.ChildContainerCreated += (sender, args) => { };
        }

        /// <summary>
        /// Clears the buildPlanTable for this instance as well as all of its descendants.
        /// </summary>
        private void ClearBuildPlans()
        {
            this.dynamicMethodsDictionary = ImmutableDictionary<Type, Tuple<DynamicMethod, IntPtr>>.Empty;
            var childrenStack = new Stack<QuickInjectContainer>();
            childrenStack.Push(this);

            while (childrenStack.Count != 0)
            {
                var curr = childrenStack.Pop();
                curr.dynamicMethodsDictionary = ImmutableDictionary<Type, Tuple<DynamicMethod, IntPtr>>.Empty;

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
                var selectedProperties = this.propertySelectorPolicy.GetProperties(t).ToArray();

                PropertyInfo[] arr;
                this.propertyInfoTable = this.propertyInfoTable.TryGetValue(t, out arr) ? this.propertyInfoTable.SetItem(t, selectedProperties) : this.propertyInfoTable.Add(t, selectedProperties);

                foreach (var selectedProperty in selectedProperties)
                {
                    selectedProperty.SetValue(existing, this.Resolve(selectedProperty.PropertyType, resolutionContext));
                }
            }
        }

        private Type GetMappingFor(Type type)
        {
            Type mappedType;

            if (this.typeMappingTable.TryGetValue(type, out mappedType))
            {
                return mappedType;
            }

            if (this.parentContainer != null)
            {
                return this.parentContainer.GetMappingFor(type);
            }

            return type;
        }

        private int GetLifetimeManagerIndexFor(Type type)
        {
            int lifetimeManagerIndex;
            if (this.lifetimeTable.TryGetValue(type, out lifetimeManagerIndex))
            {
                return lifetimeManagerIndex;
            }
            else
            {
                return -1;
            }
        }

        private LifetimeManager GetLifetimeFor(Type type)
        {
            int lifetimeManagerIndex = this.GetLifetimeManagerIndexFor(type);

            if (lifetimeManagerIndex != InvalidLifetimeIndex)
            {
                return this.lifetimeManagers[lifetimeManagerIndex];
            }

            if (this.parentContainer != null)
            {
                return this.parentContainer.GetLifetimeFor(type);
            }

            return TransientLifetimeManagerInstance;
        }

        private MethodInfo GetFactoryFor(Type type)
        {
            MethodInfo methodInfo;

            if (this.factoryTable.TryGetValue(type, out methodInfo))
            {
                return methodInfo;
            }

            return this.parentContainer?.GetFactoryFor(type);
        }

        private IEnumerable<Type> Dependencies(Type type)
        {
            Type mappedType = this.GetMappingFor(type);
            var method = this.GetFactoryFor(mappedType);

            if (method != null)
            {
                return method.GetParameters().Select(t => t.ParameterType);
            }

            // container.RegisterType<IFoo>(new LifetimeManagerWillProvideValue())
            if (mappedType.GetTypeInfo().IsInterface || mappedType.GetTypeInfo().IsAbstract)
            {
                return Enumerable.Empty<Type>();
            }

            // Regular class that can be constructed
            return mappedType.ConstructorDependencies();
        }

        private void CompileInternal(Type type, ILGenerator ilGenerator, Dictionary<Type, int> localIndexTable, int depth)
        {
            Type mappedType = this.GetMappingFor(type);
            int lifetimeManagerIndex = this.GetLifetimeManagerIndexFor(mappedType);
            MethodInfo methodInfo;

            if (this.resolutionContextType == type)
            {
                ilGenerator.Emit(OpCodes.Ldarg_0); // first argument is always resolution context
            }
            else if (this.factoryTable.TryGetValue(type, out methodInfo))
            {
                if (methodInfo == null)
                {
                    if (lifetimeManagerIndex == InvalidLifetimeIndex)
                    {
                        throw new ArgumentNullException(nameof(methodInfo)); // we use null as an indicator for RegisterInstance, but that means a lifetime manager is present
                    }

                    CompileInstanceType(ilGenerator, lifetimeManagerIndex);
                }
                else
                {
                    CompileFactoryType(ilGenerator, lifetimeManagerIndex, methodInfo, lifetimeManagerIndex == InvalidLifetimeIndex, localIndexTable);
                }
            }
            else if (mappedType.GetTypeInfo().IsAbstract || mappedType.GetTypeInfo().IsInterface)
            {
                if (lifetimeManagerIndex == InvalidLifetimeIndex)
                {
                    throw new ArgumentException("Unable to constructor type of " + mappedType.GetTypeInfo());
                }

                CompileUnconstructibleType(ilGenerator, lifetimeManagerIndex);
            }
            else
            {
                CompileConstructorBasedType(ilGenerator, lifetimeManagerIndex, mappedType.GetLongestConstructor(), this.Dependencies(type), lifetimeManagerIndex == InvalidLifetimeIndex || this.lifetimeManagers[lifetimeManagerIndex].GetType() == TransientLifetimeManagerType, this, depth, localIndexTable);
            }
        }

        private IntPtr Compile(Type typeToResolve)
        {
            if (typeToResolve == this.resolutionContextType)
            {
                throw new ArgumentException("Type asked to resolve is the resolution context");
            }

            var dynamicMethod = new DynamicMethod("Create_" + typeToResolve, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, ObjectType, ParameterTypes, QuickInjectContainerType.Module, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            Dictionary<Type, int> localIndexTable = new Dictionary<Type, int>();

            // each dependent of "typeToResolve" could be a parameterized lambda dependency which could in-turn have its own dependency
            // that's it, after this one level of extra dependency analysis we don't go further
            foreach (var lambdaDependencyType in this.GetImmediateParameterizedLambdaDependencies(typeToResolve))
            {
                // resolution context is handled elsewhere
                if (lambdaDependencyType != this.resolutionContextType)
                {
                    // NOTE: this function ultimately may recurse back into compile
                    EmitCallIndirect(this, ilGenerator, lambdaDependencyType);
                    int localIndex = ilGenerator.DeclareLocal(ObjectType).LocalIndex;
                    localIndexTable.Add(lambdaDependencyType, localIndex);
                    ilGenerator.Emit(OpCodes.Stloc, localIndex);
                }
            }

            this.CompileInternal(typeToResolve, ilGenerator, localIndexTable, 0);

            ilGenerator.Emit(OpCodes.Ret);

            var runtimeMethodHandle = (RuntimeMethodHandle)GetMethodDescriptorMethodInfo.Invoke(dynamicMethod, null);
            RuntimeHelpers.PrepareMethod(runtimeMethodHandle); // JIT

            IntPtr retVal = runtimeMethodHandle.GetFunctionPointer();
            this.dynamicMethodsDictionary.Add(typeToResolve, new Tuple<DynamicMethod, IntPtr>(dynamicMethod, retVal));

            return retVal;
        }

        private IEnumerable<Type> GetImmediateParameterizedLambdaDependencies(Type typeToResolve)
        {
            var typeSet = new HashSet<Type>();
            foreach (var type in this.Dependencies(typeToResolve))
            {
                foreach (var each in this.GetFactoryFor(type).GetParameters().Select(t => t.ParameterType))
                {
                    typeSet.Add(each);
                }
            }

            return typeSet;
        }

        private bool ShouldInline(Type type, int depth)
        {
            return false;
        }

        private sealed class InstanceInjectionMember : InjectionMember
        {
        }
    }
}