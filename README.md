The main goal of the ZeroIoC is to provide IoC for AOT platforms such as Xamarin, Unity, and Native AOT. It is powered by Roslyn Source Generator as a result executed on build and doesn't require Reflection.Emit to function.


## Get Started

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

## Features

I would say it is in the MVP stage. Under MVP, I mean that the set of features is big enough to be helpful in real projects.
This set contains:
- Multiple IoC containers can be active at the same time.
- Support for the singleton, scoped, and transient lifetimes => basic things that cover 99% of all needs.
- Powered by source generation to avoid reflection and Reflection.Emit => you can use it inside the AOT Xamarin/Unity app.
- Fast enough, with minimal overhead => the end-user of the Xamarin app will not notice a difference.

## How it works

The NuGet is deployed with the source generator and analyzer. Then it looks for class declarations that are inherited from the `` ZeroIoCContainer ``. Inside the generator looks for the `` ZeroIoCContainer.Bootstrap `` method. Based on its content, the source generator will generate another part of a partial class. For the case described above, it will look like that(skipping the performance magic):

``` cs

public partial class Container
{

    public Container()
    {
        Resolvers = Resolvers.AddOrUpdate(typeof(global::Helper), new SingletonResolver(static resolver => new global::Helper()));
        Resolvers = Resolvers.AddOrUpdate(typeof(global::IUserService), new TransientResolver(static resolver => new global::UserService(resolver.Resolve<global::Helper>())));
    }

    protected Container(ImTools.ImHashMap<Type, InstanceResolver> resolvers, ImTools.ImHashMap<Type, InstanceResolver> scopedResolvers, bool scope = false)
        : base(resolvers, scopedResolvers, scope)
    {
    }

    public override IZeroIoCResolver CreateScope()
    {
        var newScope = ScopedResolvers
            .Enumerate()
            .Aggregate(ImHashMap<Type, InstanceResolver>.Empty, (acc, o) => acc.AddOrUpdate(o.Key, o.Value.Duplicate()));
        
        return new Container(Resolvers, newScope, true);
    }
}

```

It is pretty simple stuff. The logic is based on a dictionary with `` Type `` as a key and instance resolver as a value. Such a class is generated for each separate class declaration, and because there is no static logic, you can safely define as many containers as you like.


## Limitations

Let's talk about the `` ZeroIoCContainer.Bootstrap `` method. It is not an ordinary method. It is a magic one.
It allows you to define the relations between interface and implementation, but the .net will never execute it at the runtime.
The `` ZeroIoCContainer.Bootstrap `` is just a declaration that will be parsed by source generation, and based on it, the mapping will be generated.
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
To prevent the bunch of WTF situations(and introduce a new one), I added a special analyzer that will warn you about it if you forget.

But If you want to do something at runtime, you can do it like that:
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

## Plans
- Performance improvements(it is already fast but can be better)
- Improve extensibility
- Create separate easy-to-use bootstrap nugets for common runtimes like Asp.Net Core, Xamarin, Unity3D.
