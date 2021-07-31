using System;

namespace ZeroIoC
{
    public class ServiceIsNotRegistred : Exception
    {
        public ServiceIsNotRegistred(string message)
            : base(message)
        {

        }
    }

}
