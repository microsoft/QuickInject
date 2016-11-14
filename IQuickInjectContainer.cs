// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;

    public interface IQuickInjectContainer : IServiceProvider
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

        /// <summary>
        /// Registers the instance as the subject of a type resolution.
        /// </summary>
        /// <param name="t">The type that this instance should resolve.</param>
        /// <param name="instance">The object instance.</param>
        /// <param name="lifetime">The lifetime manager.</param>
        /// <returns>This container.</returns>
        IQuickInjectContainer RegisterInstance(Type t, object instance, LifetimeManager lifetime);

        /// <summary>
        /// Notifies the container that requests for the specified type should be resolved via
        /// the resolution context.
        /// </summary>
        /// <typeparam name="T">The type to register as the context type.</typeparam>
        /// <returns>The container.</returns>
        IQuickInjectContainer RegisterTypeAsResolutionContext<T>();

        /// <summary>
        /// Registers type resolutions.
        /// </summary>
        /// <param name="from">The type that should resolved.</param>
        /// <param name="toType">The concrete type.</param>
        /// <param name="lifetimeManager">The lifetime manager.</param>
        /// <param name="injectionMember">Optional injection of an expression factory.</param>
        /// <returns>The container.</returns>
        IQuickInjectContainer RegisterType(Type from, Type toType, LifetimeManager lifetimeManager, InjectionMember injectionMember = null);

        /// <summary>
        /// Resolves the type into an object instance.
        /// </summary>
        /// <param name="t">The type to resolve.</param>
        /// <returns>The resolved object.</returns>
        object Resolve(Type t);

        T Resolve<T>();

        /// <summary>
        /// Resolves the type into an object instance.
        /// </summary>
        /// <param name="t">The type to resolve.</param>
        /// <param name="resolutionContext">The optional resolution context to use.</param>
        /// <returns>The resolved object.</returns>
        object Resolve(Type t, object resolutionContext);

        T Resolve<T>(object resolutionContext);
    }
}