using System;

namespace ZeroIoC
{
    public interface InstanceResolver : IDisposable
    {
        object Resolve(IZeroIoCResolver resolver);

        InstanceResolver Duplicate();
    }

    public sealed class TransientResolver : InstanceResolver
    {
        private readonly Func<IZeroIoCResolver, object> activator;

        public TransientResolver(Func<IZeroIoCResolver, object> activator)
        {
            this.activator = activator;
        }

        public object Resolve(IZeroIoCResolver resolver) => this.activator(resolver);

        public InstanceResolver Duplicate() => new TransientResolver(activator);

        public void Dispose() { }
    }

    public sealed class SingletonResolver : InstanceResolver
    {
        private readonly Func<IZeroIoCResolver, object> activator;
        private Func<IZeroIoCResolver, object> resolve;
        private object cache;
        private bool disposed;

        public SingletonResolver(Func<IZeroIoCResolver, object> activator)
        {
            this.activator = activator;
            this.cache = null;
            this.disposed = false;

            this.resolve = this.ResolveInternal;
        }

        private object ResolveInternal(IZeroIoCResolver resolver)
        {
            lock (activator)
            {
                this.cache = this.activator(resolver);
                this.resolve = this.GetCached;
                return cache;
            }
        }

        private object GetCached(IZeroIoCResolver resolver) => this.cache;

        public object Resolve(IZeroIoCResolver resolver) => this.resolve(resolver);

        public InstanceResolver Duplicate() => new SingletonResolver(activator);

        public void Dispose()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Instance resolver was disposed. It may happen because the scope was disposed.");
            }

            if (cache != null && cache is IDisposable disposable)
            {
                disposed = true;
                disposable.Dispose();
            }
        }
    }
}
