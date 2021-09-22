using System;
using ImTools;
using ZeroIoC.Core;

namespace ZeroIoC
{
    public abstract class ZeroIoCContainer : IZeroIoCResolver
    {
        protected ImHashMap<Type, IInstanceResolver> Resolvers = ImHashMap<Type, IInstanceResolver>.Empty;

        protected bool Scoped;

        protected ImHashMap<Type, IInstanceResolver> ScopedResolvers =
            ImHashMap<Type, IInstanceResolver>.Empty;

        protected ZeroIoCContainer()
        {
        }

        protected ZeroIoCContainer(ImHashMap<Type, IInstanceResolver> resolvers,
            ImHashMap<Type, IInstanceResolver> scopedResolvers, bool scope = false)
        {
            Resolvers = resolvers;
            ScopedResolvers = scopedResolvers;
            Scoped = scope;
        }

        public object Resolve(Type type)
        {
            var entry = Resolvers.GetValueOrDefault(type.GetHashCode(), type);
            if (entry != null)
            {
                return entry.Resolve(this);
            }

            if (Scoped)
            {
                entry = ScopedResolvers.GetValueOrDefault(type.GetHashCode(), type);
            }

            if (entry != null)
            {
                return entry.Resolve(this);
            }

            entry = ScopedResolvers.GetValueOrDefault(type.GetHashCode(), type);
            if (entry != null)
            {
                ExceptionHelper.ScopedWithoutScopeException(type.FullName);
            }

            ExceptionHelper.ServiceIsNotRegistered(type.FullName);
            return null;
        }

        public void Dispose()
        {
            foreach (var resolver in Resolvers.Enumerate())
            {
                resolver.Value.Dispose();
            }

            foreach (var resolver in ScopedResolvers.Enumerate())
            {
                resolver.Value.Dispose();
            }
        }

        public abstract IZeroIoCResolver CreateScope();

        protected abstract void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper);

        public void Merge(ZeroIoCContainer container)
        {
            foreach (var resolver in container.Resolvers.Enumerate())
            {
                Resolvers = Resolvers.AddOrUpdate(resolver.Key, resolver.Value);
            }

            foreach (var resolver in container.ScopedResolvers.Enumerate())
            {
                ScopedResolvers = ScopedResolvers.AddOrUpdate(resolver.Key, resolver.Value);
            }
        }

        public void AddDelegate(Func<IZeroIoCResolver, object> resolver, Type interfaceType, Reuse reuse = Reuse.Transient)
        {
            switch (reuse)
            {
                case Reuse.Scoped:
                    ScopedResolvers = ScopedResolvers.AddOrUpdate(interfaceType, new SingletonResolver(resolver));
                    break;
                case Reuse.Singleton:
                    Resolvers = Resolvers.AddOrUpdate(interfaceType, new SingletonResolver(resolver));
                    break;
                case Reuse.Transient:
                    Resolvers = Resolvers.AddOrUpdate(interfaceType, new TransientResolver(resolver));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reuse), reuse, null);
            }
        }

        public void AddInstance<TValue>(TValue value)
        {
            Resolvers = Resolvers.AddOrUpdate(typeof(TValue), new SingletonResolver(o => value));
        }
    }
}