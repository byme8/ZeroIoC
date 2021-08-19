# ZeroIoC IoC контейнер для .NET без рефлексії

Головна ідея цього проекту - це створити такий IoC контейнер який би чудово працював на планформах із AOT компіляцією, таких як Xamarin, Unity та Native AOT. З виходом Roslyn Source Generator-ів реалізувати це стало набагато простіше, оскільки, вони дають зручний API для того щоб аналізувати та генерити код на етапі компіляції. В результаті, можна уникнути використання рефлексії та Reflection.Emit. Що в свою чергу, відкриває можливість використовувати їх разом з AOT компіляцією.

# Як використовувати

Думаю варто почати з того як саме користуватися IoC контейнером без рефлексії в порівнянні із звичайним. Тому почнемо

1. Установіть nuget пакет ZeroIoC в проект.
```
dotnet add package ZeroIoC
```

2. Створіть контейнер який наслідується від ZeroIoCContainer і зробіть його partial класом(іншу частину згенерить кодогенератор)
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

3. Використовуйте ваш контейнер:
``` cs 
  var container = new Container();
  var userService = container.Resolve<IUserService>();
```

Як видно із прикладу нічого дивного не сталося. Так є відмінності, але все буде зрозуміло всім хто хоч колись працював з IoC контейнерами.

# Як це працює

Разом із nuget пакетом установлюється source generator і аналізатор. Source generator шукати класс який наслідувався від ``` ZeroIoCContainer ```. Потім він спробує знайти метод ``` ZeroIoCContainer.Bootstrap ```. Залежно від того що там написано source generator згенерить іншу частину partial класу. Якщо взати за основу попередній приклад, то це буде виглядати наступний чином:

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

Тут теж немає нічого складного. Вся логіка базується на словарі з ключем ``` Type ``` та інстансом резолвера як значення. Подібний класс буде згенерований для кожного окремого контейнера і оскільки тут немає ніякого статичного стану, то ми можемо створювати будь яку кулькість подібний контейнерів.


# Обмеження

Давайте розглянемо метод ``` ZeroIoCContainer.Bootstrap ```. Це не звичайни метод. На ньому вкастоватва магія.
Він дозволяє нам установити відношення між інтерфейсами та їх реалізваціями, але при цьому він не буде виконуваться в рантаймі взагалі.
Метод ``` ZeroIoCContainer.Bootstrap ``` - це лише декларація яка буде проаналізована source generat-ором і залежно від того що він там знайде, буде згенерований мапінг.
В свою чергу це означає, що немає ніякого сенсу писати в ньому будь яку іншу логіку. Розглянемо наступний приклад:
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
Всі `` if `` statement-и будуть просто проігноровані. Тому, щоб уникнути різноманітних WTF-ків(і створити новий) я створив додатковий аналізатор, який попередить що так роботи не можна.

Але якщо є необхідність щось змінити в рантацймі, то це можна зробити наступним чином:
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
Подібний підхід не потребує рефлексії і його можна безпечно використовувати навіть разом з AOT компіляцією.

# Можливості

Я б сказав що на разі ZeroIoC знаходиться на стадії MVP. Під MVP я розумію що набір можливостей достатьньо широкий щоб бути корисним в реальному проекті.

Цей набір в себе включає:
- Декілька IoC контейнерів можуть працювати одночасно.
- Підтримка singleton, scoped, та transient lifetimes => це базові речі що покривають 99% всіх ситуацій.
- Працює за рахунок source generat-ора для уникнення рефлексії та Reflection.Emit => може бути використаний разом з AOT Xamarin/Unity.
- Достатьньо швидкий з мінімальним оверхедом => користувач застосунку написаного на Xamarin не помітить різниці.


# Плани
- Покращити швидкодії(він уже досить швидкий, але я думаю може бути краще)
- Добавити більше можливостей для кастомізації
- Створити окремі easy-to-use nuget-и для популярних платформ як Asp.Net Core, Xamarin, Unity3D.

Всім дякую за увагу! Було б цікаво почути ваші думки стосовно такого підходу.
