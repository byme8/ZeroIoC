using System;

namespace ZeroIoC
{
    public interface IZeroIoCResolver : IDisposable
    {
        IZeroIoCResolver CreateScope();

        object Resolve(Type serviceType);
        object Resolve(Type type, IOverrides overrides);
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

    public static class ZeroIoCContainerExtensions
    {
        public static TService Resolve<TService>(this IZeroIoCResolver container)
        {
            return (TService)container.Resolve(typeof(TService));
        }
        
        public static TService Resolve<TService>(this IZeroIoCResolver container, IOverrides overrides)
        {
            return (TService)container.Resolve(typeof(TService), overrides);
        }

        public static void AddDelegate<TService>(this ZeroIoCContainer container, Func<IZeroIoCResolver, TService> resolver, Reuse reuse = Reuse.Transient)
        {
            container.AddDelegate(r => resolver(r), typeof(TService), reuse);
        }
        
        public static void ReplaceDelegate<TService>(this ZeroIoCContainer container, Func<IZeroIoCResolver, TService> resolver, Reuse reuse = Reuse.Transient)
        {
            container.ReplaceDelegate(r => resolver(r), typeof(TService), reuse);
        }
    }
}