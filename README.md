# ZeroIoC is reflectionless IoC Container for .NET. 

The main goal of the ZeroIoC is to provide IoC for AOT platforms such as Xamarin, Unity and Native AOT. 
It is powered by Roslyn Source Generator as result executed on build and doesn't require Reflection.Emit to function.


# Get Started

1. Install nuget package ZeroIoC to your project.
```
dotnet add package ZeroIoC
```

2. Declare your container that is inherited from ZeroIoCContainer as a partial class
``` cs

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

```

3. Use your container:
``` cs 
  var container = new Container();
  var userService = container.Resolve<IUserService>();
```

# Features

I would say it is in MVP stage. Under MVP I mean that the set of features is big enough so it can be useful in real project.
This set contains:
- Multiple IoC containers can be active in the same time.
- Support for the singleton, scoped, and transient lifetimes => basic things that cover 99% of all needs.
- Powered by source generation to avoid reflection and Reflection.Emit => can be used inside the AOT Xamarin/Unity app.
- Fast enough with minimal overhead => the end-user of the Xamarin app will not notice a difference.

# Limitations

Let's talk about the ``` ZeroIoCContainer.Bootstrap ``` method. It is not an ordinary method. It is a magic one.
it allows you to define the relations between interface and implementation but it will never be executed at runtime.
The ``` ZeroIoCContainer.Bootstrap ``` is just a declaration that will be parsed by source generation and based on it the mapping will be generated.
It means that there is no point to use statements like that:
``` cs
 public partial class Container : ZeroIoCContainer
    {
        protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
        {
            if(Config.Release)
            {
              bootstrapper.AddSingleton<IHelper, ReleaseHelper>();
            }
            else 
            {
              bootstrapper.AddSingleton<IHelper, DebugHelper>();
            }
            
            bootstrapper.AddTransient<IUserService, UserService>();
        }
    }
```
All of them will be just ignored. 
To prevent the bunch of WTF situations(and introduce a new one) I added a special analyzer that will warn you about it in case you forget.

But If you want to do something at runtime you can do it like that:
``` cs 
var container = new Container();
if(Config.Release)
{
    container.AddInstance<IHelper>(new ReleaseHelper());
}
else 
{
    container.AddInstance<IHelper>(new DebugHelper());
}

var userService = container.Resolve<IUserService>();
```
Such an approach doesn't use any reflection underhood and can be safely used inside the AOT environment.

# Plans
- Performance improvements(it already fast but can be better)
- Improve extensibilty
- Create separate easy-to-use bootstrap nugets for common runtimes like Asp.Net Core, Xamarin, Unity3D.
