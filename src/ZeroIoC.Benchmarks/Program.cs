using BenchmarkDotNet.Attributes;
using Grace.DependencyInjection;
using System;
using BenchmarkDotNet.Running;
using Grace.DependencyInjection.Lifestyle;

namespace ZeroIoC.Benchmarks
{
    public interface IUserService
    {
    }

    public class UserService : IUserService
    {
        public Guid Id { get; } = Guid.NewGuid();

        public UserService(Helper helper)
        {
        }
    }

    public class Helper
    {
    }

    public class SingleHelper
    {
        
    }

    public class SingleService
    {
        private readonly SingleHelper _helper;

        public SingleService(SingleHelper helper)
        {
            _helper = helper;
        }
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

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<IoCBenchmark>();
        }
    }

    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    public class IoCBenchmark
    {
        private DependencyInjectionContainer _grace;
        private Container _zeroioc;

        public IoCBenchmark()
        {
            _grace = new DependencyInjectionContainer();
            _grace.Configure(o =>
            {
                o.Export<SingleHelper>().As<SingleHelper>().UsingLifestyle(new SingletonLifestyle());
                o.Export<SingleService>().As<SingleService>().UsingLifestyle(new SingletonLifestyle());
                
                o.Export<Helper>().As<Helper>();
                o.Export<UserService>().As<IUserService>();
            });

            _zeroioc = new Container();
        }

        [Benchmark]
        public IUserService ZeroTransient() => (IUserService)_zeroioc.Resolve(typeof(IUserService));

        [Benchmark]
        public IUserService GraceTransient() => (IUserService)_grace.Locate(typeof(IUserService));
        
        [Benchmark]
        public SingleService ZeroSingleton() => (SingleService)_zeroioc.Resolve(typeof(SingleService));

        [Benchmark]
        public SingleService GraceSingleService() => (SingleService)_grace.Locate(typeof(SingleService));
    }
}