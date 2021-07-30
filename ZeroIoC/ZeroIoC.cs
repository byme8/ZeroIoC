using System;
using System.Collections.Generic;
using System.Reflection;

namespace ZeroIoC
{
    public interface IZeroIoCContainerBootstrapper
    {
        void AddTransient<TImplementation>();
        void AddTransient<TInterface, TImplementation>();
        void AddSingleton<TImplementation>();
        void AddSingleton<TInterface, TImplementation>();
    }

    public abstract class ZeroIoCContainer
    {
        protected static Dictionary<Type, IInstanceResolver> StaticResolvers = new Dictionary<Type, IInstanceResolver>();

        protected abstract void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper);

        public object GetService(Type type)
        {
            if (!StaticResolvers.TryGetValue(type, out var resolver))
            {
                {
                    throw new Exception($"Type {type.FullName}  is missing in resolver.");
                }
            }

            return resolver.Resolve(null);
        }
    }

    public static class ZeroIoCContainerExtensions
    {
        public static TService GetService<TService>(this ZeroIoCContainer container)
        {
            return (TService)container.GetService(typeof(TService));
        }
    }

    public interface IInstanceResolver
    {
        object Resolve(object args);
    }

    public sealed class TransientResolver : IInstanceResolver
    {
        private readonly Func<object> activator;

        public TransientResolver(Func<object> activator)
        {
            this.activator = activator;
        }

        public object Resolve(object args) => this.activator();
    }

    public sealed class SignletonResolver : IInstanceResolver
    {
        private readonly Func<object> activator;
        private object cache;

        public SignletonResolver(Func<object> activator)
        {
            this.activator = activator;
        }

        public object Resolve(object args)
        {
            if (cache is null)
            {
                lock (this)
                {
                    if (cache is null)
                    {
                        cache = this.activator();
                        return cache;
                    }
                }
            }

            return cache;
        }
    }
}
