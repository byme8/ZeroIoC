using ImTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZeroIoC.Core;

namespace ZeroIoC
{
    public abstract class ZeroIoCContainer : IZeroIoCResolver
    {
        protected ImTools.ImHashMap<Type, InstanceResolver> Resolvers = ImTools.ImHashMap<Type, InstanceResolver>.Empty;
        protected ImTools.ImHashMap<Type, InstanceResolver> ScopedResolvers = ImTools.ImHashMap<Type, InstanceResolver>.Empty;
        protected bool Scoped = false;

        protected abstract void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper);

        protected ZeroIoCContainer()
        {

        }

        protected ZeroIoCContainer(ImHashMap<Type, InstanceResolver> resolvers, ImTools.ImHashMap<Type, InstanceResolver> scopedResolvers, bool scope = false)
        {
            Resolvers = resolvers;
            ScopedResolvers = scopedResolvers;
            Scoped = scope;
        }

        public object Resolve(Type type)
        {
            var entry = Resolvers.GetValueOrDefault(type.GetHashCode(), type);
            if (entry is null)
            {
                if (Scoped)
                {
                    entry = ScopedResolvers.GetValueOrDefault(type.GetHashCode(), type);
                }

                if (entry is null)
                {
                    entry = ScopedResolvers.GetValueOrDefault(type.GetHashCode(), type);
                    if (entry != null)
                    {
                        ExceptionHelper.ScopedWithoutScopeException(type.FullName);
                    }

                    ExceptionHelper.ServiceIsNotRegistred(type.FullName);
                }
            }

            return entry.Resolve(this);
        }

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

        public void AddDelegate<TValue>(Func<IZeroIoCResolver, TValue> action)
        {
            Resolvers = Resolvers.AddOrUpdate(typeof(TValue), new SingletonResolver(o => action(o)));
        }

        public void AddInstance<TValue>(TValue value)
        {
            Resolvers = Resolvers.AddOrUpdate(typeof(TValue), new SingletonResolver(o => value));
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
    }
}