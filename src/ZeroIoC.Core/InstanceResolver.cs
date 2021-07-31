using System;

namespace ZeroIoC
{
    public interface InstanceResolver : IDisposable
    {
        object Resolve(object args);

        InstanceResolver Duplicate();
    }

    public sealed class TransientResolver : InstanceResolver
    {
        private readonly Func<object> activator;

        public TransientResolver(Func<object> activator)
        {
            this.activator = activator;
        }

        public object Resolve(object args) => this.activator();
     
        public InstanceResolver Duplicate() => new TransientResolver(activator);

        public void Dispose() { }
    }

    public sealed class SingletonResolver : InstanceResolver
    {
        private readonly Func<object> activator;
        private object cache;
        private bool _disposed;

        public SingletonResolver(Func<object> activator)
        {
            this.activator = activator;
        }

        public object Resolve(object args)
        {
            if (cache is null)
            {
                lock (this)
                {
                    if (cache is null)
                    {
                        cache = this.activator();
                        return cache;
                    }
                }
            }

            return cache;
        }

        public InstanceResolver Duplicate() => new SingletonResolver(activator);

        public void Dispose()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Instance resolver was disposed. It may happen because the scope was disposed.");
            }

            if (cache != null && cache is IDisposable disposable)
            {
                _disposed = true;
                disposable.Dispose();
            }
        }
    }
}
