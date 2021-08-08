using BenchmarkDotNet.Attributes;
using Grace.DependencyInjection;
using System;
using BenchmarkDotNet.Running;

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

    public partial class Container : ZeroIoCContainer
    {
        protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
        {
            bootstrapper.AddSingleton<Helper>();
            bootstrapper.AddTransient<IUserService, UserService>();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<IoCBenchmark>();
        }
    }

    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    public class IoCBenchmark
    {
        private DependencyInjectionContainer grace;
        private Container zeroioc;

        public IoCBenchmark()
        {
            grace = new DependencyInjectionContainer();
            grace.Configure(o =>
            {
                o.Export<Helper>().As<Helper>();
                o.Export<UserService>().As<IUserService>();
            });

            zeroioc = new Container();
        }

        [Benchmark]
        public IUserService Zero() => zeroioc.Resolve<IUserService>();

        [Benchmark]
        public IUserService Grace() => grace.Locate<IUserService>();
    }
}