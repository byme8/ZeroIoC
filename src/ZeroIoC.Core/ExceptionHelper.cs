namespace ZeroIoC;

internal static class ExceptionHelper
{
    public static void ScopedWithoutScopeException(string fullName)
    {
        throw new ScopedWithoutScopeException($"Type {fullName} is registred as scoped, but you are trying to create it without scope.");
    }

    public static void ServiceIsNotRegistered(string fullName)
    {
        throw new ServiceIsNotRegistered($"Type {fullName} is missing in resolver.");
    }
}