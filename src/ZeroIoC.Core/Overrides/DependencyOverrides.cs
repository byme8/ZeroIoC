using System;
using System.Collections.Generic;

namespace ZeroIoC.Core.Overrides
{
    public class DependencyOverrides
    {
        public Dictionary<Type, Func<object>> Overrides { get; } = new Dictionary<Type, Func<object>>();
    }
}