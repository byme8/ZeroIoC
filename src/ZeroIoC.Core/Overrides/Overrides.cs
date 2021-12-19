using ZeroIoC.Core.Overrides;

namespace ZeroIoC
{
    public class Overrides
    {
        public ConstructorOverrides Constructor { get; set; } = new ConstructorOverrides();
        public DependencyOverrides Dependency { get; set; } = new DependencyOverrides();
    }
}