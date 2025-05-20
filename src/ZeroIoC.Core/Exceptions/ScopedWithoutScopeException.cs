using System;

namespace ZeroIoC;

public class ScopedWithoutScopeException : Exception
{
    public ScopedWithoutScopeException(string message)
        : base(message)
    {

    }
}