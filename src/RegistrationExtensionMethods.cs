namespace QuickInject
{
    using System;

    public static class RegistrationExtensionMethods
    {
        public static IQuickInjectContainer RegisterType<T>(this IQuickInjectContainer container, InjectionMember injectionMember = null)
        {
            return container.RegisterType(null, typeof(T), null, injectionMember);
        }

        public static IQuickInjectContainer RegisterType<TFrom, TTo>(this IQuickInjectContainer container, InjectionMember injectionMember = null)
            where TTo : TFrom
        {
            return container.RegisterType(typeof(TFrom), typeof(TTo), null, injectionMember);
        }

        public static IQuickInjectContainer RegisterType<TFrom, TTo>(this IQuickInjectContainer container, LifetimeManager lifetimeManager, InjectionMember injectionMember = null)
            where TTo : TFrom
        {
            return container.RegisterType(typeof(TFrom), typeof(TTo), lifetimeManager, injectionMember);
        }

        public static IQuickInjectContainer RegisterType<T>(this IQuickInjectContainer container, LifetimeManager lifetimeManager, InjectionMember injectionMember = null)
        {
            return container.RegisterType(null, typeof(T), lifetimeManager, injectionMember);
        }

        public static IQuickInjectContainer RegisterType(this IQuickInjectContainer container, Type t, InjectionMember injectionMember = null)
        {
            return container.RegisterType(null, t, null, injectionMember);
        }

        public static IQuickInjectContainer RegisterType(this IQuickInjectContainer container, Type from, Type to, InjectionMember injectionMember = null)
        {
            return container.RegisterType(from, to, null, injectionMember);
        }

        public static IQuickInjectContainer RegisterType(this IQuickInjectContainer container, Type t, LifetimeManager lifetimeManager, InjectionMember injectionMember = null)
        {
            return container.RegisterType(null, t, lifetimeManager, injectionMember);
        }

        public static IQuickInjectContainer RegisterInstance<TInterface>(this IQuickInjectContainer container, TInterface instance)
        {
            return container.RegisterInstance(typeof(TInterface), instance, new ContainerControlledLifetimeManager());
        }

        public static IQuickInjectContainer RegisterInstance<TInterface>(this IQuickInjectContainer container, TInterface instance, LifetimeManager lifetimeManager)
        {
            return container.RegisterInstance(typeof(TInterface), instance, lifetimeManager);
        }

        public static IQuickInjectContainer RegisterInstance(this IQuickInjectContainer container, Type t, object instance)
        {
            return container.RegisterInstance(t, instance, new ContainerControlledLifetimeManager());
        }

        public static T Resolve<T>(this IQuickInjectContainer container)
        {
            return (T)container.Resolve(typeof(T));
        }

        public static T Resolve<T>(this IQuickInjectContainer container, object resolutionContext)
        {
            return (T)container.Resolve(typeof(T), resolutionContext);
        }
    }
}