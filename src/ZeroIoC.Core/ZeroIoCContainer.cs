using System;
using System.Collections.Generic;
using ZeroIoC.Core;

namespace ZeroIoC
{
    public abstract class ZeroIoCContainer : IZeroIoCResolver
    {
        protected Dictionary<Type, IInstanceResolver> Resolvers = new Dictionary<Type, IInstanceResolver>();

        protected bool Scoped;

        protected Dictionary<Type, IInstanceResolver> ScopedResolvers = new Dictionary<Type, IInstanceResolver>();

        protected ZeroIoCContainer()
        {
        }

        protected ZeroIoCContainer(Dictionary<Type, IInstanceResolver> resolvers,
            Dictionary<Type, IInstanceResolver> scopedResolvers, bool scope = false)
        {
            Resolvers = resolvers;
            ScopedResolvers = scopedResolvers;
            Scoped = scope;
        }

        public object Resolve(Type type)
        {
            if (Resolvers.TryGetValue(type, out var entry))
            {
                return entry.Resolve(this);
            }

            if (Scoped)
            {
                if (ScopedResolvers.TryGetValue(type, out entry))
                {
                    return entry.Resolve(this);
                }
            }

            if (ScopedResolvers.TryGetValue(type, out entry))
            {
                ExceptionHelper.ScopedWithoutScopeException(type.FullName);
            }

            ExceptionHelper.ServiceIsNotRegistered(type.FullName);
            return null;
        }

        public void Dispose()
        {
            foreach (var resolver in Resolvers.Values)
            {
                resolver.Dispose();
            }

            foreach (var resolver in ScopedResolvers.Values)
            {
                resolver.Dispose();
            }
        }

        public abstract IZeroIoCResolver CreateScope();

        protected abstract void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper);

        public void Merge(ZeroIoCContainer container)
        {
            foreach (var resolver in container.Resolvers)
            {
                Resolvers.Add(resolver.Key, resolver.Value);
            }

            foreach (var resolver in container.ScopedResolvers)
            {
                ScopedResolvers.Add(resolver.Key, resolver.Value);
            }
        }

        public void AddDelegate(Func<IZeroIoCResolver, object> resolver, Type interfaceType, Reuse reuse = Reuse.Transient)
        {
            switch (reuse)
            {
                case Reuse.Scoped:
                    ScopedResolvers.Add(interfaceType, new SingletonResolver(resolver));
                    break;
                case Reuse.Singleton:
                    Resolvers.Add(interfaceType, new SingletonResolver(resolver));
                    break;
                case Reuse.Transient:
                    Resolvers.Add(interfaceType, new TransientResolver(resolver));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reuse), reuse, null);
            }
        }

        public void AddInstance<TValue>(TValue value)
        {
            Resolvers.Add(typeof(TValue), new SingletonResolver(o => value));
        }
    }
}