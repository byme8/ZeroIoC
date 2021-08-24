using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroIoC
{
    public interface IZeroIoCResolver : IDisposable
    {
        IZeroIoCResolver CreateScope();

        object Resolve(Type serviceType);
        IEnumerable<object> ResolveMany(Type serviceType);
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

        public static IEnumerable<TService> ResolveMany<TService>(this IZeroIoCResolver container)
        {
            return container.ResolveMany(typeof(TService)).OfType<TService>();
        }

        public static void AddDelegate<TService>(this ZeroIoCContainer container, Func<IZeroIoCResolver, TService> resolver)
        {
            container.AddDelegate(r => resolver(r), typeof(TService));
        }
    }

}
