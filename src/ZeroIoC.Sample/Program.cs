using System;

namespace ZeroIoC.Sample
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
        public Guid Id { get; } = Guid.NewGuid();
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
            var container = new Container();
            var userService = container.Resolve<IUserService>();
            userService = container.Resolve<IUserService>();
            userService = container.Resolve<IUserService>();
            userService = container.Resolve<IUserService>();
        }
    }
}