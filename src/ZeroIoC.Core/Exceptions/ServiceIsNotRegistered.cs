using System;

namespace ZeroIoC
{
    public class ServiceIsNotRegistered : Exception
    {
        public ServiceIsNotRegistered(string message)
            : base(message)
        {

        }
    }
}