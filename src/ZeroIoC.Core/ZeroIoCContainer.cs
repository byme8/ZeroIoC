using System;
using System.Collections.Generic;
using System.Reflection;

namespace ZeroIoC
{
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

            return resolver.Resolve(this, null);
        }

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

        public void AddDelegate<TValue>(Func<IZeroIoCResolver, TValue> action)
        {
            Resolvers.Add(typeof(TValue), new SingletonResolver(o => action(o)));
        }
        
        public void AddInstance<TValue>(TValue value)
        {
            Resolvers.Add(typeof(TValue), new SingletonResolver(o => value));
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
    }
}