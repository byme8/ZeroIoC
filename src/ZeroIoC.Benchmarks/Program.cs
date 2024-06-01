using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Grace.DependencyInjection;
using Grace.DependencyInjection.Lifestyle;
using Microsoft.Extensions.DependencyInjection;

namespace ZeroIoC.Benchmarks
{
    public interface IUserService
    {
    }

    public class UserService : IUserService
    {
        public UserService(Helper helper)
        {
        }

        public Guid Id { get; } = Guid.NewGuid();
    }

    public class Helper
    {
    }

    public class SingleHelper
    {
    }

    public class SingleService(SingleHelper helper)
    {
        private readonly SingleHelper _helper = helper;
    }

    public partial class Container : ZeroIoCContainer
    {
        protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
        {
            bootstrapper.AddTransient<Helper>();
            bootstrapper.AddTransient<IUserService, UserService>();
            bootstrapper.AddSingleton<SingleHelper>();
            bootstrapper.AddSingleton<SingleService>();
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<IoCStartupBenchmark>();
            // BenchmarkRunner.Run<IoCRuntimeBenchmark>();
        }
    }

    public class Creators
    {
        public static Container CreateZeroIoC()
        {
            return new Container();
        }

        public static ServiceProvider CreateMicrosoft()
        {
            var services = new ServiceCollection();
            services.AddSingleton<SingleHelper>();
            services.AddSingleton<SingleService>();
            services.AddTransient<Helper>();
            services.AddTransient<IUserService, UserService>();

            return services.BuildServiceProvider();
        }

        public static DependencyInjectionContainer CreateGrace()
        {
            var grace = new DependencyInjectionContainer();
            grace.Configure(o =>
            {
                o.Export<SingleHelper>().As<SingleHelper>().UsingLifestyle(new SingletonLifestyle());
                o.Export<SingleService>().As<SingleService>().UsingLifestyle(new SingletonLifestyle());

                o.Export<Helper>().As<Helper>();
                o.Export<UserService>().As<IUserService>();
            });

            return grace;
        }
    }

    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class IoCStartupBenchmark
    {
        [Benchmark]
        public void MicrosoftStartup()
        {
            var resolver = Creators.CreateMicrosoft();
            var userService = (IUserService)resolver.GetService(typeof(IUserService));
            var singleService = (SingleService)resolver.GetService(typeof(SingleService));
        }

        [Benchmark]
        public void ZeroStartup()
        {
            var resolver = Creators.CreateZeroIoC();
            var userService = (IUserService)resolver.Resolve(typeof(IUserService));
            var singleService = (SingleService)resolver.Resolve(typeof(SingleService));
        }

        [Benchmark]
        public void GraceStartup()
        {
            var resolver = Creators.CreateGrace();
            var userService = (IUserService)resolver.Locate(typeof(IUserService));
            var singleService = (SingleService)resolver.Locate(typeof(SingleService));
        }
    }

    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class IoCRuntimeBenchmark
    {
        private readonly DependencyInjectionContainer _grace;
        private readonly Container _zeroioc;
        private readonly ServiceProvider _serviceProvider;

        public IoCRuntimeBenchmark()
        {
            _grace = Creators.CreateGrace();
            _serviceProvider = Creators.CreateMicrosoft();
            _zeroioc = Creators.CreateZeroIoC();
        }

        [Benchmark]
        public IUserService MicrosoftTransient()
        {
            return (IUserService)_serviceProvider.GetService(typeof(IUserService));
        }

        [Benchmark]
        public IUserService ZeroTransient()
        {
            return (IUserService)_zeroioc.Resolve(typeof(IUserService));
        }

        [Benchmark]
        public IUserService GraceTransient()
        {
            return (IUserService)_grace.Locate(typeof(IUserService));
        }

        [Benchmark]
        public SingleService MicrosoftSingleton()
        {
            return (SingleService)_serviceProvider.GetService(typeof(SingleService));
        }

        [Benchmark]
        public SingleService ZeroSingleton()
        {
            return (SingleService)_zeroioc.Resolve(typeof(SingleService));
        }

        [Benchmark]
        public SingleService GraceSingleton()
        {
            return (SingleService)_grace.Locate(typeof(SingleService));
        }
    }
}