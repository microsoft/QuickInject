namespace QuickInject
{
    using System;

    public interface IQuickInjectContainer : IServiceProvider, IDisposable
    {
        /// <summary>
        /// Gets the parent container or returns null if no parent exists. 
        /// </summary>
        IQuickInjectContainer Parent { get; }

        /// <summary>
        /// Initializes the extension. 
        /// </summary>
        /// <param name="extension">The extension to be initialized.</param>
        /// <returns>Returns this container.</returns>
        IQuickInjectContainer AddExtension(QuickInjectExtension extension);

        /// <summary>
        /// Sets the new IPropertySelectorPolicy to use. 
        /// </summary>
        /// <param name="policy">The policy to use.</param>
        void SetPropertySelectorPolicy(IPropertySelectorPolicy policy);

        /// <summary>
        /// Builds up the properties of an existing object. 
        /// </summary>
        /// <param name="t">The type of the object.</param>
        /// <param name="existing">The existing object.</param>
        /// <returns>The built-up object.</returns>
        object BuildUp(Type t, object existing);

        /// <summary>
        /// Builds up the properties of an existing object. 
        /// </summary>
        /// <param name="t">The type of the object.</param>
        /// <param name="existing">The existing object.</param>
        /// <param name="resolutionContext">The resolution context to use for build-up.</param>
        /// <returns>The built-up object.</returns>
        object BuildUp(Type t, object existing, object resolutionContext);

        /// <summary>
        /// Creates a child container instance.
        /// </summary>
        /// <returns>The new child container instance.</returns>
        IQuickInjectContainer CreateChildContainer();

        IQuickInjectContainer RegisterInstance(Type t, object instance, LifetimeManager lifetime);

        IQuickInjectContainer RegisterType(Type from, Type to, LifetimeManager lifetimeManager, InjectionMember injectionMember = null);

        object Resolve(Type t);
        
        object Resolve(Type t, object resolutionContext);
        
        /// <summary>
        /// Adds a build plan visitor this container.
        /// </summary>
        /// <param name="visitor"></param>
        void AddBuildPlanVisitor(IBuildPlanVisitor visitor);

        /// <summary>
        /// Registers a listener that can be used to visualize the full object graph that was needed to 
        /// compute a given type. Currently does nothing? 
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        void RegisterDependencyTreeListener(Action<ITreeNode<Type>> action);

        void RegisterResolutionContextType<T>();
    }
}