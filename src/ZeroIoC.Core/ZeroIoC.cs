using System;
using System.Collections.Generic;

namespace ZeroIoC
{
    public interface IZeroIoCResolver : IDisposable
    {
        IZeroIoCResolver CreateScope();

        object Resolve(Type serviceType);
    }

    public interface IZeroIoCContainerBootstrapper
    {

        void AddTransient<TImplementation>();
        void AddTransient<TInterface, TImplementation>();
        void AddSingleton<TImplementation>();
        void AddSingleton<TInterface, TImplementation>();
        void AddScoped<TImplementation>();
        void AddScoped<TInterface, TImplementation>();
    }

    public abstract class ZeroIoCContainer : IZeroIoCResolver
    {
        protected Dictionary<Type, InstanceResolver> Resolvers = new Dictionary<Type, InstanceResolver>();
        protected Dictionary<Type, InstanceResolver> ScopedResolvers = new Dictionary<Type, InstanceResolver>();
        protected bool Scoped = false;

        protected abstract void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper);

        protected ZeroIoCContainer()
        {

        }

        protected ZeroIoCContainer(Dictionary<Type, InstanceResolver> resolvers, Dictionary<Type, InstanceResolver> scopedResolvers, bool scope = false)
        {
            Resolvers = resolvers;
            ScopedResolvers = scopedResolvers;
            Scoped = scope;
        }

        public object Resolve(Type type)
        {
            if (!Resolvers.TryGetValue(type, out var resolver))
            {
                if (ScopedResolvers.TryGetValue(type, out resolver) && !Scoped)
                {
                    throw new ScopedWithoutScopeException($"Type {type.FullName} is registred as scoped, but you are trying to create it without scope.");
                }

                if (resolver is null)
                {
                    throw new ServiceIsNotRegistred($"Type {type.FullName} is missing in resolver.");
                }
            }

            return resolver.Resolve(null);
        }

        public void Dispose()
        {
            Resolvers.Clear();
            foreach (var resolver in ScopedResolvers.Values)
            {
                resolver.Dispose();
            }
            ScopedResolvers.Clear();
        }

        public abstract IZeroIoCResolver CreateScope();
    }

    public static class ZeroIoCContainerExtensions
    {
        public static TService Resolve<TService>(this IZeroIoCResolver container)
        {
            return (TService)container.Resolve(typeof(TService));
        }
    }

}
