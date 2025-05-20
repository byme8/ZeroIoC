using System;
using ZeroIoC.Core.Overrides;

namespace ZeroIoC;

public interface IOverrides
{
    ConstructorOverrides Constructor { get; set; }
    DependencyOverrides Dependency { get; set; }
}

public interface IOverrides<in T> : IOverrides
{

}

internal class Override : IOverrides
{
    public ConstructorOverrides Constructor { get; set; } = new ConstructorOverrides();
    public DependencyOverrides Dependency { get; set; } = new DependencyOverrides();
}
    
public static class Overrides
{
    public static IOverrides Create()
    {
        return new Override();
    }
}
    
public static class OverridesExtensions
{
    public static IOverrides Dependency<TDependency>(this IOverrides overrides, Func<TDependency> func)
    {
        return overrides.Dependency<IOverrides, TDependency>(func);
    }
        
    public static TOverrides Dependency<TOverrides, TDependency>(this TOverrides overrides, Func<TDependency> func)
        where TOverrides : IOverrides
    {
        overrides.Dependency.Overrides.Add(typeof(TDependency), () => func());
        return overrides;
    }
    
    public static IOverrides Constructor(this IOverrides overrides, params (string ArgumentName, object ArgumentValue)[] values)
    {
        return overrides.Constructor<IOverrides>(values);
    }
        
    public static TOverrides Constructor<TOverrides>(this TOverrides overrides, params (string ArgumentName, object ArgumentValue)[] values)
        where TOverrides : IOverrides
    {
        foreach (var value in values)
        {
            overrides.Constructor.Overrides.Add(value.ArgumentName, value.ArgumentValue);
        }
            
        return overrides;
    }
}