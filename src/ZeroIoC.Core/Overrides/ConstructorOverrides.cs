using System.Collections.Generic;

namespace ZeroIoC.Core.Overrides;

public class ConstructorOverrides
{
    public Dictionary<string, object> Overrides { get; } = new Dictionary<string, object>();
}