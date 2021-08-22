using System;

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

    public static class ZeroIoCContainerExtensions
    {
        public static TService Resolve<TService>(this IZeroIoCResolver container)
        {
            return (TService)container.Resolve(typeof(TService));
        }
    }

}
