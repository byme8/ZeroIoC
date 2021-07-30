using System;

namespace ZeroIoC.Sample
{

    public interface IUserService
    {

    }

    public class UserService : IUserService
    {

    }

    public class Helper { }

    public partial class Container : ZeroIoCContainer<Container>
    {
        protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
        {
            bootstrapper.AddSingleton<Helper>();
            bootstrapper.AddSingleton<IUserService, UserService>();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var container = new Container();
            var userService = container.Resolve<IUserService>();
        }
    }
}
