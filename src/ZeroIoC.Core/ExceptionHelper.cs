namespace ZeroIoC.Core
{
    static class ExceptionHelper
    {
        public static void ScopedWithoutScopeException(string fullName)
        {
            throw new ScopedWithoutScopeException($"Type {fullName} is registred as scoped, but you are trying to create it without scope.");
        }

        public static void ServiceIsNotRegistred(string fullName)
        {
            throw new ServiceIsNotRegistred($"Type {fullName} is missing in resolver.");
        }

    }
}
